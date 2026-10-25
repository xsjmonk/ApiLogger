namespace ApiLogger;

public sealed class ApiPayloadLogger<TPayload> : IApiPayloadLogger<TPayload>
{
    private readonly IApiLogQueue<TPayload> _queue;

    public ApiPayloadLogger(IApiLogQueue<TPayload> queue)
    {
        _queue = queue;
    }

    public void Log(DateTime timestamp, ApiLogKind kind, TPayload payload)
    {
        Log(new ApiLogItem<TPayload>(timestamp, kind, payload));
    }

    public void Log(ApiLogItem<TPayload> item)
    {
        try
        {
            _queue.Enqueue(item);
        }
        catch
        {
            // Adapter failures should not break request handling.
        }
    }
}

