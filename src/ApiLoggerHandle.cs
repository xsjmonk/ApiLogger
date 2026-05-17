using Microsoft.Extensions.Logging;

namespace ApiLogger;

public sealed class ApiLoggerHandle : IDisposable, IAsyncDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IApiLogRuntime<ApiLogMessagePayload> _runtime;
    private int _stopCalled;
    private int _disposed;

    internal ApiLoggerHandle(ILogger logger, ILoggerFactory loggerFactory, IApiLogRuntime<ApiLogMessagePayload> runtime)
    {
        Logger = logger;
        _loggerFactory = loggerFactory;
        _runtime = runtime;
    }

    public ILogger Logger { get; }
    public ILoggerFactory LoggerFactory => _loggerFactory;

    public Task FlushAsync(CancellationToken cancellationToken = default)
        => _runtime.FlushAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _stopCalled, 1) == 1)
            return Task.CompletedTask;
        return _runtime.StopAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;
        StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        _loggerFactory.Dispose();
        _runtime.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
        _loggerFactory.Dispose();
        _runtime.Dispose();
    }
}
