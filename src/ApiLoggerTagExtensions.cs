using Microsoft.Extensions.Logging;

namespace ApiLogger;

public static class ApiLoggerTagExtensions
{
    private const string TagTokenPrefix = "[[TAG:";
    private const string TagTokenSuffix = "]]";
    private const string TagParamTokenPrefix = "[[TAGPARAM:";

    /// <summary>
    /// Logs the given message to a per-tag log file:
    /// {LogFileNameWithoutExt}.{tag}{LogFileExtension} (in the same log folder).
    /// </summary>
    public static void LogWithTag(
        this ILogger logger,
        LogLevel logLevel,
        string tag,
        string message,
        string? tagParam = null,
        Exception? exception = null)
    {
        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        var token = string.IsNullOrWhiteSpace(tag)
            ? string.Empty
            : $"{TagTokenPrefix}{tag}{TagTokenSuffix}";

        if (!string.IsNullOrWhiteSpace(tagParam))
        {
            token += $"{TagParamTokenPrefix}{tagParam}{TagTokenSuffix}";
        }

        var finalMessage = string.IsNullOrWhiteSpace(token)
            ? message ?? string.Empty
            : $"{token} {message ?? string.Empty}";

        // The ApiLoggerAdapter ignores EventId for the payload formatting,
        // but ApiLogger writer will route based on the tag token at the start.
        logger.Log(logLevel, new EventId(0, tag ?? string.Empty), message, exception,
            (state, ex) => finalMessage);
    }
}

