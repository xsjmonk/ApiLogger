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
        var shortCategoryName = ShortenCategoryName(categoryName);

        var finalMessage = string.IsNullOrWhiteSpace(shortCategoryName)
            ? message
            : $"[{shortCategoryName}] {message}";

        return new ApiLogMessagePayload(finalMessage, exception);
    }

    private static string ShortenCategoryName(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return categoryName;

        // Logger category names are usually fully-qualified type names.
        // Keep only the last segment for compact logs.
        // Examples:
        // - System.Net.Http.HttpClient.PicRemote.ClientHandler => ClientHandler
        // - Outer+Inner => Inner
        var idxDot = categoryName.LastIndexOf('.');
        var idxPlus = categoryName.LastIndexOf('+');
        var idx = Math.Max(idxDot, idxPlus);
        return idx >= 0 && idx < categoryName.Length - 1
            ? categoryName[(idx + 1)..]
            : categoryName;
    }
}
