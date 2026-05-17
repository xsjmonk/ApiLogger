namespace ApiLogger;

public interface IApiLogPayloadFactory<TPayload>
{
    TPayload Create(
        DateTime timestamp,
        Microsoft.Extensions.Logging.LogLevel logLevel,
        Microsoft.Extensions.Logging.EventId eventId,
        string categoryName,
        string message,
        Exception? exception);
}

