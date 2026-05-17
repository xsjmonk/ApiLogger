namespace ApiLogger.Tests;

public sealed class TestWarningCaptureLogger : IApiPayloadLogger<TestPayload>
{
    private readonly Action<ApiLogItem<TestPayload>> _onLog;

    public TestWarningCaptureLogger(Action<ApiLogItem<TestPayload>> onLog)
    {
        _onLog = onLog;
    }

    public void Log(DateTime timestamp, ApiLogKind kind, TestPayload payload)
    {
        _onLog(new ApiLogItem<TestPayload>(timestamp, kind, payload));
    }

    public void Log(ApiLogItem<TestPayload> item)
    {
        _onLog(item);
    }
}