using System;

namespace ApiLogger;

public enum ApiLogKind
{
    Info = 0,
    Error = 1,
    Warning = 2,
}

public sealed class ApiLogItem<TPayload>
{
    public ApiLogItem(
        DateTime timestamp,
        ApiLogKind kind,
        TPayload payload)
    {
        Timestamp = timestamp;
        Kind = kind;
        Payload = payload;
    }

    public DateTime Timestamp { get; }
    public ApiLogKind Kind { get; }
    public TPayload Payload { get; }

    public static ApiLogItem<TPayload> Create(DateTime timestamp, ApiLogKind kind, TPayload payload)
        => new(timestamp, kind, payload);
}

public interface IApiLogFormatter<TPayload>
{
    string Format(ApiLogItem<TPayload> item);
}

