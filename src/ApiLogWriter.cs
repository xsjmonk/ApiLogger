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
    private readonly Encoding _encoding;
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
    private struct BufferedLine
    {
        public string Line;
        public string ActiveFileName;

        public BufferedLine(string line, string activeFileName)
        {
            Line = line;
            ActiveFileName = activeFileName;
        }
    }

    // Buffered lines now also track which "active file" they belong to.
    // This enables tagged logging without changing public ILogger interfaces.
    private readonly List<BufferedLine> _bufferLines = [];
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

        _encoding = ResolveEncoding(options.Encoding);

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

        // Tagged routing:
        // If the formatted payload starts with our tag token, write this entry into:
        //   LogFileName + "." + tag + extension
        // while keeping the same rotation/cleanup logic.
        var (activeFileName, cleanedLine) = TryExtractTagAndCleanLine(line);
        var lineWithEol = cleanedLine + Environment.NewLine;

        // Track bytes for rotation threshold; do not assume 1 char == 1 byte.
        var lineBytes = _encoding.GetByteCount(lineWithEol);

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
            _bufferLines.Add(new BufferedLine(lineWithEol, activeFileName));
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

        try
        {
            var currentLengths = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            // Correctness-first: rotate while writing buffered lines so each active file
            // cannot grow far beyond _rotateSizeBytes just because the flush batch is large.
            foreach (var entry in _bufferLines)
            {
                var activeFileName = entry.ActiveFileName;
                var activeFilePath = GetActiveFilePath(activeFileName);

                if (!currentLengths.TryGetValue(activeFileName, out var currentLength))
                {
                    currentLength = File.Exists(activeFilePath)
                        ? new FileInfo(activeFilePath).Length
                        : 0L;
                }

                var lineBytes = _encoding.GetByteCount(entry.Line);

                if (currentLength > 0 && currentLength + lineBytes > _rotateSizeBytes)
                {
                    RotateActiveFile(activeFilePath, activeFileName);
                    currentLength = 0L;
                }

                await File.AppendAllTextAsync(activeFilePath, entry.Line, _encoding, cancellationToken);
                currentLength += lineBytes;
                currentLengths[activeFileName] = currentLength;
            }

            _bufferLines.Clear();
            _pendingBytes = 0;
            _pendingItems = 0;

            // Cleanup for all involved active files (default + tag files).
            foreach (var kv in currentLengths)
            {
                CleanupOldLogFiles(kv.Key);
            }
        }
        catch (Exception ex)
        {
            // Keep the pipeline resilient; do not clear buffer on IO failure so a later flush can try again.
            _logger.LogError(ex, "ApiLogWriter append/rotate failed (resilient).");
        }
    }

    private string GetActiveFilePath(string activeFileName)
    {
        return Path.Combine(_logDir, activeFileName);
    }

    private string GetRotatedFilePath(string activeFileName, int index)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(activeFileName);
        var ext = Path.GetExtension(activeFileName);
        return Path.Combine(_logDir, $"{nameWithoutExt}.{index}{ext}");
    }

    private int? TryParseRotatedLogIndex(string activeFileName, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var activeNameWithoutExt = Path.GetFileNameWithoutExtension(activeFileName);
        var activeExt = Path.GetExtension(activeFileName);
        var rotatedPrefix = $"{activeNameWithoutExt}.";
        var rotatedSuffix = activeExt;

        if (!fileName.StartsWith(rotatedPrefix, StringComparison.OrdinalIgnoreCase) ||
            !fileName.EndsWith(rotatedSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(fileName, activeFileName, StringComparison.OrdinalIgnoreCase))
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

        var rotatedPath = GetNextRotatedFilePath(_activeFileName);
        File.Move(activeFilePath, rotatedPath);
        await Task.CompletedTask;
    }

    private void RotateActiveFile(string activeFilePath)
    {
        RotateActiveFile(activeFilePath, _activeFileName);
    }

    private void RotateActiveFile(string activeFilePath, string activeFileName)
    {
        try
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

            var rotatedPath = GetNextRotatedFilePath(activeFileName);
            File.Move(activeFilePath, rotatedPath);
        }
        catch
        {
        }
    }

    private int GetNextRotatedFilePathIndex(string activeFileName)
    {
        try
        {
            var activeNameWithoutExt = Path.GetFileNameWithoutExtension(activeFileName);
            var activeExt = Path.GetExtension(activeFileName);
            var searchPattern = $"{activeNameWithoutExt}.*{activeExt}";

            var maxIndex = 0;
            foreach (var file in Directory.EnumerateFiles(_logDir, searchPattern))
            {
                var name = Path.GetFileName(file);
                var idx = TryParseRotatedLogIndex(activeFileName, name);
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

    private string GetNextRotatedFilePath(string activeFileName)
    {
        var nextIndex = GetNextRotatedFilePathIndex(activeFileName);
        return GetRotatedFilePath(activeFileName, nextIndex);
    }

    private void CleanupOldLogFiles(string activeFileName)
    {
        try
        {
            var activeNameWithoutExt = Path.GetFileNameWithoutExtension(activeFileName);
            var activeExt = Path.GetExtension(activeFileName);
            var searchPattern = $"{activeNameWithoutExt}.*{activeExt}";

            var rotatedFiles = Directory.EnumerateFiles(_logDir, searchPattern)
                .Select(path => new { Path = path, Index = TryParseRotatedLogIndex(activeFileName, Path.GetFileName(path)) })
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

    private const string TagTokenPrefix = "[[TAG:";
    private const string TagTokenSuffix = "]]";
    private const string TagParamTokenPrefix = "[[TAGPARAM:";

    private (string ActiveFileName, string CleanedLine) TryExtractTagAndCleanLine(string formattedLine)
    {
        // Default routing: keep existing behavior.
        var activeFileName = _activeFileName;
        var cleanedLine = formattedLine;

        // Formatter output is tab-separated.
        // For ApiLogMessagePayload (ILogger), DefaultApiLogMessageFormatter outputs:
        //   timestamp \t kind \t message \t exceptionType \t exceptionMessage \t exceptionStackTrace
        //
        // For other payload formatters, it might output only:
        //   timestamp \t kind \t payloadText
        //
        // To support all of these without hard dependency on formatter type,
        // we treat "the message/payloadText column" as the segment after the second '\t'.
        var firstTab = formattedLine.IndexOf('\t');
        if (firstTab < 0)
        {
            return (activeFileName, cleanedLine);
        }

        var secondTab = formattedLine.IndexOf('\t', firstTab + 1);
        if (secondTab < 0)
        {
            return (activeFileName, cleanedLine);
        }

        var thirdTab = formattedLine.IndexOf('\t', secondTab + 1);
        var payloadStart = secondTab + 1;
        var payloadEnd = thirdTab < 0 ? formattedLine.Length : thirdTab;
        if (payloadEnd <= payloadStart)
        {
            return (activeFileName, cleanedLine);
        }

        var payloadText = formattedLine.Substring(payloadStart, payloadEnd - payloadStart);

        // ApiLogMessagePayloadFactory commonly prefixes messages with:
        //   [{shortCategoryName}] {message}
        // so the token may not be the first characters in payloadText.
        var tokenIdx = payloadText.StartsWith(TagTokenPrefix, StringComparison.Ordinal)
            ? 0
            : payloadText.IndexOf(TagTokenPrefix, StringComparison.Ordinal);

        if (tokenIdx >= 0)
        {
            // Allow token either at the beginning, or immediately after the category prefix: "] "
            if (tokenIdx != 0)
            {
                if (tokenIdx < 2 || payloadText[tokenIdx - 2] != ']' || payloadText[tokenIdx - 1] != ' ')
                {
                    return (activeFileName, cleanedLine);
                }
            }

            var tagStart = tokenIdx + TagTokenPrefix.Length;
            var tagEnd = payloadText.IndexOf(TagTokenSuffix, tagStart, StringComparison.Ordinal);
            if (tagEnd > tagStart)
            {
                var tag = payloadText.Substring(tagStart, tagEnd - tagStart);
                var safeTag = MakeSafeFileNamePart(tag);
                if (!string.IsNullOrWhiteSpace(safeTag))
                {
                    // Strip optional tag-param token(s) from payload text.
                    var prefix = payloadText.Substring(0, tokenIdx); // includes optional category prefix
                    var rest = payloadText.Substring(tagEnd + TagTokenSuffix.Length).TrimStart();

                    if (rest.StartsWith(TagParamTokenPrefix, StringComparison.Ordinal))
                    {
                        var paramStart = TagParamTokenPrefix.Length;
                        var paramEnd = rest.IndexOf(TagTokenSuffix, paramStart, StringComparison.Ordinal);
                        if (paramEnd > paramStart)
                        {
                            rest = rest.Substring(paramEnd + TagTokenSuffix.Length).TrimStart();
                        }
                    }

                    cleanedLine = formattedLine.Substring(0, payloadStart) + prefix + rest + formattedLine.Substring(payloadEnd);
                    activeFileName = BuildTaggedActiveFileName(safeTag);
                }
            }
        }

        return (activeFileName, cleanedLine);
    }

    private string BuildTaggedActiveFileName(string tag)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(_activeFileName);
        var ext = Path.GetExtension(_activeFileName);
        return $"{nameWithoutExt}.{tag}{ext}";
    }

    private static string MakeSafeFileNamePart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
        }

        var safe = sb.ToString().Trim();
        return string.IsNullOrWhiteSpace(safe) ? string.Empty : safe;
    }

    private static Encoding ResolveEncoding(string encodingName)
    {
        if (string.IsNullOrWhiteSpace(encodingName))
            return new UTF8Encoding(false);

        var normalized = encodingName.Trim().ToLowerInvariant();

        if (normalized is "utf-8" or "utf8")
            return new UTF8Encoding(false);

        try
        {
            return Encoding.GetEncoding(encodingName.Trim());
        }
        catch
        {
            return new UTF8Encoding(false);
        }
    }
}

