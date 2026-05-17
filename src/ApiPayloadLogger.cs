using Microsoft.Extensions.Logging;

namespace ApiLogger;

public sealed class ApiPayloadLogger<TPayload> : IApiPayloadLogger<TPayload>
{
    private readonly IApiLogQueue<TPayload> _queue;
    private readonly ILogger<ApiPayloadLogger<TPayload>> _logger;

    public ApiPayloadLogger(
        IApiLogQueue<TPayload> queue,
        ILogger<ApiPayloadLogger<TPayload>> logger)
    {
        _queue = queue;
        _logger = logger;
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
        catch (Exception ex)
        {
            // Adapter failures should not break request handling.
            _logger.LogError(ex, "ApiPayloadLogger enqueue failed (resilient).");
        }
    }
}

