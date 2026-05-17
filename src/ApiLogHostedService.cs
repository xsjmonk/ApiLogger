using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiLogger;

public sealed class ApiLogHostedService<TPayload> : IHostedService, IDisposable
{
    private readonly ApiLogQueue<TPayload> _queue;
    private readonly IApiLogWriter<TPayload> _writer;
    private readonly ILogger<ApiLogHostedService<TPayload>> _logger;

    // Used only for abnormal forced disposal. Normal StopAsync drains via channel completion.
    private CancellationTokenSource? _disposeCts;
    private Task? _executingTask;

    public ApiLogHostedService(
        ApiLogQueue<TPayload> queue,
        IApiLogWriter<TPayload> writer,
        ILogger<ApiLogHostedService<TPayload>> logger)
    {
        _queue = queue;
        _writer = writer;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        _disposeCts = new CancellationTokenSource();
        _executingTask = Task.Run(() => ExecuteLoopAsync(_disposeCts.Token));
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Normal shutdown path:
        //  1) stop producing new items (channel completion)
        //  2) drain everything already queued
        //  3) best-effort flush the writer
        try
        {
            _queue.Complete();
        }
        catch
        {
        }

        try
        {
            if (_executingTask is not null)
            {
                await _executingTask.WaitAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Host is timing out; fall through to best-effort flush below.
        }
        catch
        {
        }

        try
        {
            // If the host cancellation token is already canceled, flush with None as best-effort.
            var flushToken = cancellationToken.IsCancellationRequested ? CancellationToken.None : cancellationToken;
            await _writer.FlushAsync(flushToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApiLogHostedService FlushAsync failed (resilient).");
        }
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
                                _logger.LogError(ex, "ApiLogHostedService write failed (resilient).");
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
                            _logger.LogError(ex, "ApiLogHostedService flush marker failed (resilient).");
                            entry.FlushCompletion?.TrySetResult();
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during forced disposal.
        }
    }

    public void Dispose()
    {
        try
        {
            _disposeCts?.Cancel();
            _disposeCts?.Dispose();
        }
        catch
        {
        }
    }
}

