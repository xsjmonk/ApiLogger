using System;
using System.Globalization;

namespace ApiLogger;

public sealed class ApiLogOptions
{
    public string LogDir { get; set; } = string.Empty;
    public string LogFileName { get; set; } = string.Empty;
    public string RotateSize { get; set; } = "10MB";
    public long RotateSizeBytes { get; set; } = 10 * 1024 * 1024;
    public string Encoding { get; set; } = "utf-8";

    public static long ParseRotateSizeBytesOrDefault(string? value, long defaultBytes)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultBytes;
        }

        var raw = value.Trim();
        if (raw.Length < 3)
        {
            return defaultBytes;
        }

        // Support only KB / MB with optional whitespace between number and suffix.
        var upper = raw.ToUpperInvariant();
        var suffixLength = 0;
        if (upper.EndsWith("KB", StringComparison.Ordinal))
        {
            suffixLength = 2;
        }
        else if (upper.EndsWith("MB", StringComparison.Ordinal))
        {
            suffixLength = 2;
        }
        else
        {
            return defaultBytes;
        }

        var numberPart = raw[..^suffixLength].Trim();
        if (!long.TryParse(numberPart, NumberStyles.None, CultureInfo.InvariantCulture, out var n))
        {
            return defaultBytes;
        }

        if (n <= 0)
        {
            return defaultBytes;
        }

        try
        {
            if (upper.EndsWith("KB", StringComparison.Ordinal))
            {
                return checked(n * 1024);
            }

            // MB
            return checked(n * 1024 * 1024);
        }
        catch (OverflowException)
        {
            return defaultBytes;
        }
    }
}

