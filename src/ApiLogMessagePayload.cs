using System;

namespace ApiLogger;

/// <summary>
/// Ready-made payload for simple string-based logging with optional exception details.
/// </summary>
public sealed class ApiLogMessagePayload
{
    public ApiLogMessagePayload(string message, Exception? exception = null)
    {
        Message = message ?? string.Empty;
        ExceptionType = exception?.GetType().FullName;
        ExceptionMessage = exception?.Message;
        ExceptionStackTrace = exception?.StackTrace;
    }

    /// <summary>
    /// The log message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The full name of the exception type, or null if no exception.
    /// </summary>
    public string? ExceptionType { get; }

    /// <summary>
    /// The exception message, or null if no exception.
    /// </summary>
    public string? ExceptionMessage { get; }

    /// <summary>
    /// The exception stack trace, or null if no exception.
    /// </summary>
    public string? ExceptionStackTrace { get; }
}