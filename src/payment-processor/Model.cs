using System.Text.Json.Serialization;

public readonly record struct ConfigurationTokenSetWireRequest(string Token);

public readonly record struct PaymentWireRequest(Guid CorrelationId, decimal Amount, DateTime RequestedAt);

public readonly record struct MessageWireResponse(string Message);

public readonly record struct PaymentWireResponse(Guid CorrelationId, decimal Amount, DateTime RequestedAt);

public readonly record struct PaymentsSummaryWireResponse(decimal TotalAmount, int TotalRequests, decimal TotalFee, decimal FeePerTransaction);

public readonly record struct ConfigurationDelaySetWireRequest(int Delay);

public readonly record struct ConfigurationFailureSetWireRequest(bool Failure);

public readonly record struct ConfigurationSetWireResponse<T>(string Config, T Was, T Is);

public readonly record struct ServicesAvailabilityWireResponse(bool Failing, int MinResponseTime);

public class ConfigurationToken
{
    public string ApplicationToken { get; set; }

    public ConfigurationToken(string token)
    {
        ApplicationToken = token;
    }
}


public class ConfigurationHttpResponseDelay
{
    public int HttpResponseDelay { get; set; }

    public ConfigurationHttpResponseDelay(int delay)
    {
        HttpResponseDelay = delay;
    }
}


public class ConfigurationHttpResponseFailure
{
    public bool HttpResponseFailure { get; set; }

    public ConfigurationHttpResponseFailure(bool failure)
    {
        HttpResponseFailure = failure;
    }
}

[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(ConfigurationTokenSetWireRequest))]
[JsonSerializable(typeof(PaymentWireRequest))]
[JsonSerializable(typeof(PaymentWireResponse))]
[JsonSerializable(typeof(MessageWireResponse))]
[JsonSerializable(typeof(PaymentsSummaryWireResponse))]
[JsonSerializable(typeof(ConfigurationDelaySetWireRequest))]
[JsonSerializable(typeof(ConfigurationFailureSetWireRequest))]
[JsonSerializable(typeof(ConfigurationSetWireResponse<long>))]
[JsonSerializable(typeof(ConfigurationSetWireResponse<bool>))]
[JsonSerializable(typeof(ConfigurationSetWireResponse<int>))]
[JsonSerializable(typeof(ServicesAvailabilityWireResponse))]
public partial class AppJsonSerializerContext 
    : JsonSerializerContext { }
