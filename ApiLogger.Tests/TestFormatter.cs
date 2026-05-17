using System.Globalization;

namespace ApiLogger.Tests;

public sealed class TestFormatter : IApiLogFormatter<TestPayload>
{
    public string Format(ApiLogItem<TestPayload> item)
    {
        return string.Join('\t',
            item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
            item.Kind.ToString(),
            item.Payload.Message.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' '));
    }
}