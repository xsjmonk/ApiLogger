using System.Threading.Channels;

namespace ApiLogger;

internal enum ApiLogQueueEntryKind
{
    Log = 0,
    Flush = 1,
}

internal sealed class ApiLogQueueEntry<TPayload>
{
    private ApiLogQueueEntry(
        ApiLogQueueEntryKind kind,
        ApiLogItem<TPayload>? item,
        TaskCompletionSource? flushCompletion)
    {
        Kind = kind;
        Item = item;
        FlushCompletion = flushCompletion;
    }

    public ApiLogQueueEntryKind Kind { get; }
    public ApiLogItem<TPayload>? Item { get; }
    public TaskCompletionSource? FlushCompletion { get; }

    public static ApiLogQueueEntry<TPayload> CreateLog(ApiLogItem<TPayload> item)
        => new(ApiLogQueueEntryKind.Log, item, null);

    public static ApiLogQueueEntry<TPayload> CreateFlush(TaskCompletionSource completion)
        => new(ApiLogQueueEntryKind.Flush, null, completion);
}

public sealed class ApiLogQueue<TPayload> : IApiLogQueue<TPayload>
{
    private readonly Channel<ApiLogQueueEntry<TPayload>> _channel;

    public ApiLogQueue()
    {
        _channel = Channel.CreateUnbounded<ApiLogQueueEntry<TPayload>>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
        });
    }

    public void Enqueue(ApiLogItem<TPayload> item)
    {
        if (!_channel.Writer.TryWrite(ApiLogQueueEntry<TPayload>.CreateLog(item)))
        {
            // Resilient: never throw from enqueue.
        }
    }

    internal Task EnqueueFlushMarkerAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_channel.Writer.TryWrite(ApiLogQueueEntry<TPayload>.CreateFlush(completion)))
        {
            return Task.CompletedTask;
        }

        return cancellationToken.CanBeCanceled
            ? completion.Task.WaitAsync(cancellationToken)
            : completion.Task;
    }

    internal ChannelReader<ApiLogQueueEntry<TPayload>> Reader => _channel.Reader;

    internal void Complete()
    {
        try
        {
            _channel.Writer.TryComplete();
        }
        catch
        {
        }
    }
}

