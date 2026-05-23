using Microsoft.Extensions.Logging;

namespace ApiLogger;

/// <summary>
/// Ready-made logger implementation that wraps IApiPayloadLogger<ApiLogMessagePayload>.
/// </summary>
public sealed class ApiLogger : IApiLogger, IDisposable
{
    private readonly IApiPayloadLogger<ApiLogMessagePayload> _payloadLogger;
    private bool _disposed;

    public ApiLogger(IApiPayloadLogger<ApiLogMessagePayload> payloadLogger)
    {
        _payloadLogger = payloadLogger ?? throw new ArgumentNullException(nameof(payloadLogger));
    }

    public void LogInfo(string info)
    {
        if (_disposed) return;
        _payloadLogger.Log(ApiLogTimestamps.Now, ApiLogKind.Info, new ApiLogMessagePayload(info));
    }

    public void LogWarning(string warning)
    {
        if (_disposed) return;
        _payloadLogger.Log(ApiLogTimestamps.Now, ApiLogKind.Warning, new ApiLogMessagePayload(warning));
    }

    public void LogError(string error)
    {
        if (_disposed) return;
        _payloadLogger.Log(ApiLogTimestamps.Now, ApiLogKind.Error, new ApiLogMessagePayload(error));
    }

    public void LogError(string error, Exception ex)
    {
        if (_disposed) return;
        _payloadLogger.Log(ApiLogTimestamps.Now, ApiLogKind.Error, new ApiLogMessagePayload(error, ex));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Note: We don't dispose the underlying payloadLogger as it's managed by DI
    }
}