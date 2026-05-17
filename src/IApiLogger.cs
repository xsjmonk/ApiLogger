using System;

namespace ApiLogger;

/// <summary>
/// Ready-made logger for simple info/warning/error logging.
/// </summary>
public interface IApiLogger
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="info">The informational message to log.</param>
    void LogInfo(string info);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="warning">The warning message to log.</param>
    void LogWarning(string warning);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="error">The error message to log.</param>
    void LogError(string error);

    /// <summary>
    /// Logs an error message with exception details.
    /// </summary>
    /// <param name="error">The error message to log.</param>
    /// <param name="ex">The exception to log.</param>
    void LogError(string error, Exception ex);
}