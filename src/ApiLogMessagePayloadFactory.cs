using Microsoft.Extensions.Logging;

namespace ApiLogger;

public sealed class ApiLogMessagePayloadFactory : IApiLogPayloadFactory<ApiLogMessagePayload>
{
    public ApiLogMessagePayload Create(
        DateTime timestamp,
        LogLevel logLevel,
        EventId eventId,
        string categoryName,
        string message,
        Exception? exception)
    {
        var finalMessage = string.IsNullOrWhiteSpace(categoryName)
            ? message
            : $"[{categoryName}] {message}";

        return new ApiLogMessagePayload(finalMessage, exception);
    }
}
