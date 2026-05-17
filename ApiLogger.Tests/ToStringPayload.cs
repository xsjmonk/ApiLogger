namespace ApiLogger.Tests;

public sealed class ToStringPayload
{
    public ToStringPayload(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => "payload:" + Value;
}