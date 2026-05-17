using System.Globalization;

namespace ApiLogger;

/// <summary>
/// Default formatter for ApiLogMessagePayload that writes tab-separated values.
/// </summary>
public sealed class DefaultApiLogMessageFormatter : IApiLogFormatter<ApiLogMessagePayload>
{
    public string Format(ApiLogItem<ApiLogMessagePayload> item)
    {
        if (item == null)
        {
            return string.Empty;
        }

        var timestamp = item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var kind = item.Kind.ToString();
        var message = Sanitize(item.Payload.Message);
        var exceptionType = Sanitize(item.Payload.ExceptionType);
        var exceptionMessage = Sanitize(item.Payload.ExceptionMessage);
        var exceptionStackTrace = Sanitize(item.Payload.ExceptionStackTrace);

        return string.Join('\t', timestamp, kind, message, exceptionType, exceptionMessage, exceptionStackTrace);
    }

    private static string Sanitize(string? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        return value
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('\t', ' ');
    }
}