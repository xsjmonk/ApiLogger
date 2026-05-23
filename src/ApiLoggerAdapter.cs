using Microsoft.Extensions.Logging;

namespace ApiLogger;

public sealed class ApiLoggerAdapter<TPayload> : ILogger
{
    private readonly IApiPayloadLogger<TPayload> _payloadLogger;
    private readonly IApiLogPayloadFactory<TPayload> _payloadFactory;
    private readonly string _categoryName;

    public ApiLoggerAdapter(
        IApiPayloadLogger<TPayload> payloadLogger,
        IApiLogPayloadFactory<TPayload> payloadFactory,
        string categoryName)
    {
        _payloadLogger = payloadLogger;
        _payloadFactory = payloadFactory;
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        var timestamp = ApiLogTimestamps.Now;
        var payload = _payloadFactory.Create(timestamp, logLevel, eventId, _categoryName, message, exception);

        var kind = logLevel switch
        {
            LogLevel.Error or LogLevel.Critical => ApiLogKind.Error,
            LogLevel.Warning => ApiLogKind.Warning,
            _ => ApiLogKind.Info,
        };

        _payloadLogger.Log(timestamp, kind, payload);
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose()
        {
        }
    }
}

