using System;
using Microsoft.Extensions.Logging;

namespace ApiLogger.Tests;

/// <summary>
/// Simple payload factory for testing.
/// </summary>
public sealed class TestPayloadFactory : IApiLogPayloadFactory<TestPayload>
{
    public TestPayload Create(DateTime timestamp, LogLevel logLevel, EventId eventId, string categoryName, string message, Exception? exception)
    {
        return new TestPayload(message);
    }
}