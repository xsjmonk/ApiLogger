namespace ApiLogger;

public interface IApiPayloadLogger<TPayload>
{
    void Log(DateTime timestamp, ApiLogKind kind, TPayload payload);
    void Log(ApiLogItem<TPayload> item);
}

