namespace ApiLogger;

/// <summary>
/// Represents a self-owned API log runtime for console/non-host applications.
/// Use this when there is no IHostedService to manage the logger lifetime.
/// Call StopAsync() or Dispose() before process exit to ensure queued items are flushed.
/// </summary>
public interface IApiLogRuntime<TPayload> : IApiPayloadLogger<TPayload>, IDisposable
{
    /// <summary>
    /// Flushes all queued log items accepted before this call to disk.
    /// Returns when the items have been written and flushed.
    /// </summary>
    Task FlushAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the runtime, drains queued items, flushes the writer, and disposes resources.
    /// After this call, Log() calls become no-op.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}