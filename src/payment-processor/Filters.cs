
public class CustomTokenAuth
    : IEndpointFilter
{
    private readonly ConfigurationToken _password;
    private readonly ILogger<CustomTokenAuth> _logger;


    public CustomTokenAuth(ILogger<CustomTokenAuth> logger, ConfigurationToken password)
    {
        _logger = logger;
        _password = password;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.HttpContext.Request.Headers["X-Rinha-Token"] != _password.ApplicationToken)
        {
            _logger.LogWarning("Headers: {Headers}", context.HttpContext.Request.Headers);
            _logger.LogWarning("Unauthorized access attempt with token: {Token}", context.HttpContext.Request.Headers["X-Rinha-Token"].ToString() ?? "undefined");
            return Results.Unauthorized();
        }

        return await next(context);
    }
}

public class MaybeSimulateDelay
    : IEndpointFilter
{
    private readonly ConfigurationHttpResponseDelay _delay;

    public MaybeSimulateDelay(ConfigurationHttpResponseDelay delay)
    {
        _delay = delay;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (_delay.HttpResponseDelay > 0)
        {
            await Task.Delay(_delay.HttpResponseDelay);
        }

        return await next(context);
    }
}


public class MaybeSimulateFailure
    : IEndpointFilter
{
    private readonly ConfigurationHttpResponseFailure _failure;

    public MaybeSimulateFailure(ConfigurationHttpResponseFailure failure)
    {
        _failure = failure;
    }
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (_failure.HttpResponseFailure)
        {
            return Results.StatusCode(500);
        }

        return await next(context);
    }
}
