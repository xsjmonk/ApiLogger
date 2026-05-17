using System.Text;
using Microsoft.Extensions.Logging;

namespace ApiLogger;

public interface IApiLogWriter<TPayload>
{
    ValueTask WriteAsync(ApiLogItem<TPayload> item, CancellationToken cancellationToken = default);
    Task FlushAsync(CancellationToken cancellationToken = default);
}

public sealed class ApiLogWriter<TPayload> : IApiLogWriter<TPayload>, IDisposable
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    private const int TotalRetainedRotatedFiles = 4; // active + up to 4 rotated

    private const int FlushIntervalMilliseconds = 1000;
    private const int FlushBufferBytes = 64 * 1024;
    private const int MaxBufferedItems = 1024;

    private readonly string _logDir;
    private readonly string _activeFileName;
    private readonly long _rotateSizeBytes;
    private readonly IApiLogFormatter<TPayload> _formatter;
    private readonly ILogger<ApiLogWriter<TPayload>> _logger;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<string> _bufferLines = [];
    private long _pendingBytes;
    private int _pendingItems;

    private readonly Timer _flushTimer;
    // 0 = not disposed-started, 1 = dispose-started
    private int _disposeStarted;
    private volatile bool _disposed;

    public ApiLogWriter(
        ApiLogOptions options,
        IApiLogFormatter<TPayload> formatter,
        ILogger<ApiLogWriter<TPayload>> logger)
    {
        _logDir = string.IsNullOrWhiteSpace(options.LogDir) ? AppContext.BaseDirectory : options.LogDir;
        _activeFileName = string.IsNullOrWhiteSpace(options.LogFileName) ? "app.txt" : options.LogFileName;
        _rotateSizeBytes = options.RotateSizeBytes > 0 ? options.RotateSizeBytes : 10 * 1024 * 1024;
        _formatter = formatter;
        _logger = logger;

        Directory.CreateDirectory(_logDir);

        _flushTimer = new Timer(_ =>
        {
            // Fire-and-forget periodic flush.
            // Guard against disposal so callbacks don't race with gate disposal.
            if (Volatile.Read(ref _disposeStarted) == 1)
            {
                return;
            }

            _ = Task.Run(() => FlushAsync(CancellationToken.None));
        }, null, FlushIntervalMilliseconds, FlushIntervalMilliseconds);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) == 1)
        {
            return;
        }

        try
        {
            _flushTimer.Dispose();
        }
        catch
        {
        }

        // Best-effort flush buffered data.
        try
        {
            FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        catch
        {
        }

        _disposed = true;
        _gate.Dispose();
    }

    public async ValueTask WriteAsync(ApiLogItem<TPayload> item, CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _disposeStarted) == 1 || _disposed)
        {
            return;
        }

        var line = _formatter.Format(item);
        var lineWithEol = line + Environment.NewLine;

        // Track bytes for rotation threshold; do not assume 1 char == 1 byte.
        var lineBytes = Utf8WithoutBom.GetByteCount(lineWithEol);

        var lockTaken = false;
        try
        {
            await _gate.WaitAsync(cancellationToken);
            lockTaken = true;
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        try
        {
            _bufferLines.Add(lineWithEol);
            _pendingBytes += lineBytes;
            _pendingItems++;

            var shouldFlush = _pendingBytes >= FlushBufferBytes
                               || _pendingItems >= MaxBufferedItems;

            if (!shouldFlush)
            {
                return;
            }

            await FlushCoreAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Keep request logging resilient.
            _logger.LogError(ex, "ApiLogWriter write/flush failed (resilient).");
        }
        finally
        {
            if (lockTaken)
            {
                try
                {
                    _gate.Release();
                }
                catch
                {
                }
            }
        }
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return;
        }

        var lockTaken = false;
        try
        {
            await _gate.WaitAsync(cancellationToken);
            lockTaken = true;
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        try
        {
            if (_pendingItems == 0)
            {
                return;
            }

            await FlushCoreAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApiLogWriter FlushAsync failed (resilient).");
        }
        finally
        {
            if (lockTaken)
            {
                try
                {
                    _gate.Release();
                }
                catch
                {
                }
            }
        }
    }

    private async Task FlushCoreAsync(CancellationToken cancellationToken)
    {
        if (_pendingItems == 0)
        {
            return;
        }

        var activeFilePath = GetActiveFilePath();
        try
        {
            var currentLength = File.Exists(activeFilePath)
                ? new FileInfo(activeFilePath).Length
                : 0L;

            // Correctness-first: rotate while writing buffered lines so the active file
            // cannot grow far beyond _rotateSizeBytes just because the flush batch is large.
            foreach (var line in _bufferLines)
            {
                var lineBytes = Utf8WithoutBom.GetByteCount(line);

                if (currentLength > 0 && currentLength + lineBytes > _rotateSizeBytes)
                {
                    RotateActiveFile(activeFilePath);
                    currentLength = 0L;
                }

                await File.AppendAllTextAsync(activeFilePath, line, Utf8WithoutBom, cancellationToken);
                currentLength += lineBytes;
            }

            _bufferLines.Clear();
            _pendingBytes = 0;
            _pendingItems = 0;

            CleanupOldLogFiles();
        }
        catch (Exception ex)
        {
            // Keep the pipeline resilient; do not clear buffer on IO failure so a later flush can try again.
            _logger.LogError(ex, "ApiLogWriter append/rotate failed (resilient).");
        }
    }

    private string GetActiveFilePath()
    {
        return Path.Combine(_logDir, _activeFileName);
    }

    private string GetRotatedFilePath(int index)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(_activeFileName);
        var ext = Path.GetExtension(_activeFileName);
        return Path.Combine(_logDir, $"{nameWithoutExt}.{index}{ext}");
    }

    private int? TryParseRotatedLogIndex(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var activeNameWithoutExt = Path.GetFileNameWithoutExtension(_activeFileName);
        var activeExt = Path.GetExtension(_activeFileName);
        var rotatedPrefix = $"{activeNameWithoutExt}.";
        var rotatedSuffix = activeExt;

        if (!fileName.StartsWith(rotatedPrefix, StringComparison.OrdinalIgnoreCase) ||
            !fileName.EndsWith(rotatedSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(fileName, _activeFileName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var middle = fileName.Substring(
            rotatedPrefix.Length,
            fileName.Length - rotatedPrefix.Length - rotatedSuffix.Length);

        if (string.IsNullOrWhiteSpace(middle))
        {
            return null;
        }

        return int.TryParse(middle, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var idx)
            && idx > 0
            ? idx
            : null;
    }

    private async Task RotateActiveFileIfNeededAsync(string activeFilePath, long pendingBatchBytes, CancellationToken cancellationToken)
    {
        if (!File.Exists(activeFilePath))
        {
            return;
        }

        var info = new FileInfo(activeFilePath);
        var activeLength = info.Length;
        if (activeLength <= 0)
        {
            return;
        }

        if (activeLength + pendingBatchBytes <= _rotateSizeBytes)
        {
            return;
        }

        var rotatedPath = GetNextRotatedFilePath();
        File.Move(activeFilePath, rotatedPath);
        await Task.CompletedTask;
    }

    private void RotateActiveFile(string activeFilePath)
    {
        if (!File.Exists(activeFilePath))
        {
            return;
        }

        var activeLength = new FileInfo(activeFilePath).Length;
        if (activeLength <= 0)
        {
            return;
        }

        var rotatedPath = GetNextRotatedFilePath();
        File.Move(activeFilePath, rotatedPath);
    }

    private int GetNextRotatedFilePathIndex()
    {
        try
        {
            var activeNameWithoutExt = Path.GetFileNameWithoutExtension(_activeFileName);
            var activeExt = Path.GetExtension(_activeFileName);
            var searchPattern = $"{activeNameWithoutExt}.*{activeExt}";

            var maxIndex = 0;
            foreach (var file in Directory.EnumerateFiles(_logDir, searchPattern))
            {
                var name = Path.GetFileName(file);
                var idx = TryParseRotatedLogIndex(name);
                if (idx.HasValue && idx.Value > maxIndex)
                {
                    maxIndex = idx.Value;
                }
            }

            return maxIndex == 0 ? 1 : maxIndex + 1;
        }
        catch
        {
            return 1;
        }
    }

    private string GetNextRotatedFilePath()
    {
        var nextIndex = GetNextRotatedFilePathIndex();
        return GetRotatedFilePath(nextIndex);
    }

    private void CleanupOldLogFiles()
    {
        try
        {
            var activeNameWithoutExt = Path.GetFileNameWithoutExtension(_activeFileName);
            var activeExt = Path.GetExtension(_activeFileName);
            var searchPattern = $"{activeNameWithoutExt}.*{activeExt}";

            var rotatedFiles = Directory.EnumerateFiles(_logDir, searchPattern)
                .Select(path => new { Path = path, Index = TryParseRotatedLogIndex(Path.GetFileName(path)) })
                .Where(x => x.Index.HasValue)
                .Select(x => new { x.Path, Index = x.Index!.Value })
                .OrderByDescending(x => x.Index)
                .ToList();

            // Keep up to 4 newest rotated files.
            foreach (var old in rotatedFiles.Skip(TotalRetainedRotatedFiles))
            {
                try
                {
                    File.Delete(old.Path);
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }
}

