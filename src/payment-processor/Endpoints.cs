using System.Net.Mime;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Npgsql;
using Scalar.AspNetCore;

var initialToken = Environment.GetEnvironmentVariable("INITIAL_TOKEN") ?? "123";
var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? "Host=localhost;Port=5432;Database=rinha;Username=postgres;Password=postgres;Minimum Pool Size=15;Maximum Pool Size=20;Connection Pruning Interval=3";
var transactionFee = decimal.Parse(Environment.GetEnvironmentVariable("TRANSACTION_FEE") ?? "0.05");
var rateLimitWindow = int.Parse(Environment.GetEnvironmentVariable("RATE_LIMIT_SECONDS") ?? "5");

var configurationHttpResponseDelay = new ConfigurationHttpResponseDelay(0);
var configurationHttpResponseFailure = new ConfigurationHttpResponseFailure(false);
var applicationToken = new ConfigurationToken(initialToken);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddNpgsqlDataSource(dbConnectionString);
builder.Services.AddSingleton(configurationHttpResponseDelay);
builder.Services.AddSingleton(configurationHttpResponseFailure);
builder.Services.AddSingleton(applicationToken);
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("service-health", opt =>
    {
        opt.PermitLimit = 1;
        opt.Window = TimeSpan.FromSeconds(rateLimitWindow);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
    options.OnRejected = async (context, cancellationToken) =>
     {
         context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
         context.HttpContext.Response.Headers.RetryAfter = rateLimitWindow.ToString();
         await context.HttpContext.Response.WriteAsync("Calm down, bitch. Try again later.", cancellationToken);
     };
});

builder.Services.AddOpenApi("v1", options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
});

var app = builder.Build();

// app.MapScalarApiReference();

app.MapOpenApi();

app.MapScalarApiReference();

app.MapGet("/", async (HttpContext context) =>
{
    context.Response.ContentType = MediaTypeNames.Text.Html;
    await context.Response.WriteAsync(File.ReadAllText("index.html"));
});

app.MapGet("/payments/service-health", (
ConfigurationHttpResponseFailure failure,
    ConfigurationHttpResponseDelay delay)
    => new ServicesAvailabilityWireResponse(
        failure.HttpResponseFailure,
        delay.HttpResponseDelay))
        .RequireRateLimiting("service-health");

app.MapPost("/payments", async (
    ILogger<Endpoint> logger,
    NpgsqlDataSource dataSource,
    PaymentWireRequest paymentRequest) =>
{
    try
    {
        await using var cmd = dataSource.CreateCommand();
        cmd.CommandText = @"INSERT INTO payments (correlationId, amount, requested_at)
                            VALUES (@correlationId, @amount, @requested_at);";
        cmd.Parameters.AddWithValue("correlationId", paymentRequest.CorrelationId);
        cmd.Parameters.AddWithValue("amount", paymentRequest.Amount);
        cmd.Parameters.AddWithValue("requested_at", paymentRequest.RequestedAt);
        await cmd.ExecuteNonQueryAsync();
        return Results.Ok(new { Message = $"payment processed successfully" });
    }
    catch (PostgresException ex)
    {
        if (ex.MessageText.Contains("duplicate key value violates unique constraint", StringComparison.CurrentCultureIgnoreCase))
        {
            var warning = $"Payment could not be processed. CorrelationId already exists: {paymentRequest.CorrelationId}. Error details: {ex}";
            logger.LogWarning(warning);
            return Results.UnprocessableEntity(new { Message = warning });
        }
        else
        {
            logger.LogError(ex, "postgres exception");
            return Results.InternalServerError(new { Message = ex.ToString() });
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "error");
        return Results.InternalServerError(new { Message = ex.ToString() });
    }
}).AddEndpointFilter<MaybeSimulateDelay>()
.AddEndpointFilter<MaybeSimulateFailure>();

app.MapGet("/payments/{id}", async (
    NpgsqlDataSource dataSource,
    Guid id) =>
{
    await using var cmd = dataSource.CreateCommand();
    cmd.CommandText = @"SELECT correlationId, amount, requested_at FROM payments where correlationId = @correlationId;";
    cmd.Parameters.AddWithValue("@correlationId", id);
    await using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        var correlationId = reader.GetGuid(0);
        var amount = reader.GetDecimal(1);
        var requestedAt = reader.GetDateTime(2);
        return Results.Ok(new PaymentWireResponse(id, amount, requestedAt));
    }
    return Results.NotFound();
});

app.MapGet("/admin/payments-summary", async (
    NpgsqlDataSource dataSource,
    DateTime? from,
    DateTime? to) =>
{
    await using var cmd = dataSource.CreateCommand();
    cmd.CommandText = @"WITH summary AS (
                            SELECT correlationId, amount
                            FROM payments
                            WHERE requested_at BETWEEN @from AND @to
                               OR @from IS NULL
                               OR @to IS NULL
                        )
                        SELECT COUNT(correlationId), COALESCE(SUM(amount), 0)
                        FROM summary;";
    cmd.Parameters.AddWithValue("@from", from.HasValue ? from.Value : DBNull.Value);
    cmd.Parameters.AddWithValue("@to", to.HasValue ? to.Value : DBNull.Value);
    await using var reader = await cmd.ExecuteReaderAsync();
    await reader.ReadAsync();

    var totalRequests = reader.GetInt32(0);
    var totalAmount = reader.GetDecimal(1);

    return Results.Ok(new PaymentsSummaryWireResponse(
        totalAmount,
        totalRequests,
        totalRequests * transactionFee,
        transactionFee));

}).AddEndpointFilter<CustomTokenAuth>();

app.MapPut("/admin/configurations/token", (
    ConfigurationToken token,
    ConfigurationTokenSetWireRequest request) =>
{
    token.ApplicationToken = request.Token;
    return Results.NoContent();
}).AddEndpointFilter<CustomTokenAuth>(); ;

app.MapPut("/admin/configurations/delay", (
    ConfigurationHttpResponseDelay configurationHttpResponseDelay,
    ConfigurationDelaySetWireRequest request) =>
{
    var response = new ConfigurationSetResponse(
        "delay",
         configurationHttpResponseDelay.HttpResponseDelay,
         request.Delay);
    configurationHttpResponseDelay.HttpResponseDelay = request.Delay;
    return Results.Ok(response);
}).AddEndpointFilter<CustomTokenAuth>();

app.MapPut("/admin/configurations/failure", (
    ConfigurationHttpResponseFailure configurationHttpResponseFailure,
    ConfigurationFailureSetWireRequest request) =>
{
    var response = new ConfigurationSetResponse(
        "failure",
         configurationHttpResponseFailure.HttpResponseFailure,
         request.Failure);
    configurationHttpResponseFailure.HttpResponseFailure = request.Failure;
    return Results.Ok(response);

}).AddEndpointFilter<CustomTokenAuth>();

app.MapPost("/admin/purge-payments", async (
    NpgsqlDataSource dataSource) =>
{
    await using var cmd = dataSource.CreateCommand();
    cmd.CommandText = @"TRUNCATE TABLE payments RESTART IDENTITY;";
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { message = "All payments purged." });

}).AddEndpointFilter<CustomTokenAuth>();

app.UseRateLimiter();

app.Run();

public class Endpoint { }
