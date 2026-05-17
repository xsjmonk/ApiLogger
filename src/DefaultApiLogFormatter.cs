using System.Globalization;

namespace ApiLogger;

public sealed class DefaultApiLogFormatter<TPayload> : IApiLogFormatter<TPayload>
{
    public string Format(ApiLogItem<TPayload> item)
    {
        if (item == null)
        {
            return string.Empty;
        }

        var timestamp = item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var kind = item.Kind.ToString();

        var payloadText = item.Payload?.ToString();
        if (payloadText == null)
        {
            payloadText = string.Empty;
        }
        else
        {
            payloadText = payloadText
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Replace('\t', ' ');
        }

        return string.Join('\t', timestamp, kind, payloadText);
    }
}
