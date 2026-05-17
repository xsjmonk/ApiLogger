using Microsoft.Extensions.Logging;

namespace ApiLogger;

public sealed class ApiLoggerProvider<TPayload> : ILoggerProvider
{
    private readonly IApiPayloadLogger<TPayload> _payloadLogger;
    private readonly IApiLogPayloadFactory<TPayload> _payloadFactory;

    public ApiLoggerProvider(
        IApiPayloadLogger<TPayload> payloadLogger,
        IApiLogPayloadFactory<TPayload> payloadFactory)
    {
        _payloadLogger = payloadLogger;
        _payloadFactory = payloadFactory;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new ApiLoggerAdapter<TPayload>(_payloadLogger, _payloadFactory, categoryName);
    }

    public void Dispose()
    {
        // No unmanaged resources.
    }
}

