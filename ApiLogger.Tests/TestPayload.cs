namespace ApiLogger.Tests;

public sealed class TestPayload
{
    public TestPayload(string message)
    {
        Message = message;
    }

    public string Message { get; }

    public override string ToString() => Message;
}