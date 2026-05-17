using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace ApiLogger;

internal sealed class NoOpLogger<T> : ILogger<T>
{
    public static readonly NoOpLogger<T> Instance = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => false;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
    }
}

public sealed class ApiLogRuntime<TPayload> : IApiLogRuntime<TPayload>
{
    private readonly ApiLogQueue<TPayload> _queue;
    private readonly ApiLogWriter<TPayload> _writer;
    private readonly ApiPayloadLogger<TPayload> _loggerAdapter;
    private readonly ILogger<ApiLogRuntime<TPayload>> _logger;
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly Task _executingTask;
    private int _stopStarted;
    private int _disposed;

    public ApiLogRuntime(
        ApiLogOptions options,
        IApiLogFormatter<TPayload> formatter,
        ILoggerFactory? loggerFactory = null)
    {
        _queue = new ApiLogQueue<TPayload>();

        var writerLogger = loggerFactory != null
            ? loggerFactory.CreateLogger<ApiLogWriter<TPayload>>()
            : NoOpLogger<ApiLogWriter<TPayload>>.Instance;
        _writer = new ApiLogWriter<TPayload>(options, formatter, writerLogger);

        var adapterLogger = loggerFactory != null
            ? loggerFactory.CreateLogger<ApiPayloadLogger<TPayload>>()
            : NoOpLogger<ApiPayloadLogger<TPayload>>.Instance;
        _loggerAdapter = new ApiPayloadLogger<TPayload>(_queue, adapterLogger);

        _logger = loggerFactory != null
            ? loggerFactory.CreateLogger<ApiLogRuntime<TPayload>>()
            : NoOpLogger<ApiLogRuntime<TPayload>>.Instance;

        _executingTask = Task.Run(() => ExecuteLoopAsync(_disposeCts.Token));
    }

    public void Log(DateTime timestamp, ApiLogKind kind, TPayload payload)
    {
        Log(new ApiLogItem<TPayload>(timestamp, kind, payload));
    }

    public void Log(ApiLogItem<TPayload> item)
    {
        if (Volatile.Read(ref _stopStarted) == 1 || Volatile.Read(ref _disposed) == 1)
        {
            return;
        }

        _loggerAdapter.Log(item);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _stopStarted) == 1 || Volatile.Read(ref _disposed) == 1)
        {
            await _writer.FlushAsync(cancellationToken);
            return;
        }

        try
        {
            await _queue.EnqueueFlushMarkerAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApiLogRuntime queue flush marker failed (resilient).");
            await _writer.FlushAsync(cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _stopStarted, 1) == 1)
        {
            return;
        }

        try
        {
            _queue.Complete();
        }
        catch
        {
        }

        try
        {
            await _executingTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }

        try
        {
            var flushToken = cancellationToken.IsCancellationRequested ? CancellationToken.None : cancellationToken;
            await _writer.FlushAsync(flushToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApiLogRuntime StopAsync flush failed (resilient).");
        }

        _writer.Dispose();
    }

    private async Task ExecuteLoopAsync(CancellationToken cancellationToken)
    {
        var reader = _queue.Reader;
        try
        {
            while (await reader.WaitToReadAsync(cancellationToken))
            {
                while (reader.TryRead(out var entry))
                {
                    if (entry.Kind == ApiLogQueueEntryKind.Log)
                    {
                        if (entry.Item is not null)
                        {
                            try
                            {
                                await _writer.WriteAsync(entry.Item, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "ApiLogRuntime write failed (resilient).");
                            }
                        }
                        continue;
                    }

                    if (entry.Kind == ApiLogQueueEntryKind.Flush)
                    {
                        try
                        {
                            await _writer.FlushAsync(cancellationToken);
                            entry.FlushCompletion?.TrySetResult();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "ApiLogRuntime flush marker failed (resilient).");
                            entry.FlushCompletion?.TrySetResult();
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        try
        {
            StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        catch
        {
        }

        try
        {
            _disposeCts.Cancel();
            _disposeCts.Dispose();
        }
        catch
        {
        }
    }
}