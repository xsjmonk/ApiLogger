using Microsoft.Extensions.Logging;

namespace ApiLogger;

public sealed class ApiLoggerProvider<TPayload> : ILoggerProvider
{
    private static readonly ILogger s_internalNullLogger = new InternalNullLogger();

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
        // Avoid DI/recursion deadlock: internal components use ILogger<T> while this provider is registering.
        if (categoryName.StartsWith("ApiLogger", StringComparison.Ordinal))
        {
            return s_internalNullLogger;
        }

        return new ApiLoggerAdapter<TPayload>(_payloadLogger, _payloadFactory, categoryName);
    }

    public void Dispose()
    {
        // No unmanaged resources.
    }

    private sealed class InternalNullLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }
}

