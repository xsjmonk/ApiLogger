using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace ApiLogger;

internal static class ApiLogOptionsFactory
{
    private const long DefaultRotateSizeBytes = 10 * 1024 * 1024;

    public static ApiLogOptions CreateNormalized(Action<ApiLogOptions> configure)
    {
        var options = new ApiLogOptions();
        configure(options);
        options.RotateSizeBytes = ApiLogOptions.ParseRotateSizeBytesOrDefault(options.RotateSize, DefaultRotateSizeBytes);
        NormalizeFileName(options);
        return options;
    }

    public static ApiLogOptions CreateNormalized(ApiLogOptions options)
    {
        var normalized = new ApiLogOptions
        {
            LogDir = options.LogDir,
            LogFileName = options.LogFileName,
            RotateSize = options.RotateSize,
            RotateSizeBytes = options.RotateSizeBytes > 0
                ? options.RotateSizeBytes
                : ApiLogOptions.ParseRotateSizeBytesOrDefault(options.RotateSize, DefaultRotateSizeBytes)
        };
        NormalizeFileName(normalized);
        return normalized;
    }

    public static ApiLogOptions CreateDefault()
    {
        var fileName = ResolveDefaultFileName();
        return new ApiLogOptions
        {
            LogDir = string.Empty,
            LogFileName = fileName,
            RotateSize = "10MB",
            RotateSizeBytes = DefaultRotateSizeBytes
        };
    }

    public static ApiLogOptions CreateNormalized(IConfiguration configuration)
    {
        var section = configuration.GetSection("ApiLogger");
        var options = new ApiLogOptions();

        var logDir = section["LogDir"];
        options.LogDir = string.IsNullOrWhiteSpace(logDir) ? string.Empty : logDir.Trim();

        var logFileName = section["LogFileName"];
        options.LogFileName = string.IsNullOrWhiteSpace(logFileName) ? string.Empty : logFileName.Trim();

        var rotateSize = section["RotateSize"];
        options.RotateSize = string.IsNullOrWhiteSpace(rotateSize) ? "10MB" : rotateSize;

        var rotateSizeBytesStr = section["RotateSizeBytes"];
        if (long.TryParse(rotateSizeBytesStr, NumberStyles.None, CultureInfo.InvariantCulture, out var bytes) && bytes > 0)
        {
            options.RotateSizeBytes = bytes;
        }
        else
        {
            options.RotateSizeBytes = ApiLogOptions.ParseRotateSizeBytesOrDefault(options.RotateSize, DefaultRotateSizeBytes);
        }

        NormalizeFileName(options);
        return options;
    }

    internal static void NormalizeFileName(ApiLogOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.LogFileName))
        {
            options.LogFileName = ResolveDefaultFileName();
            return;
        }

        var fileName = Path.GetFileName(options.LogFileName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            options.LogFileName = ResolveDefaultFileName();
            return;
        }

        options.LogFileName = fileName;
    }

    private static string ResolveDefaultFileName()
    {
        var processPath = Environment.ProcessPath;
        string baseName;

        if (!string.IsNullOrWhiteSpace(processPath))
        {
            baseName = Path.GetFileNameWithoutExtension(processPath);
        }
        else
        {
            var friendlyName = AppDomain.CurrentDomain.FriendlyName;
            baseName = string.IsNullOrWhiteSpace(friendlyName)
                ? string.Empty
                : Path.GetFileNameWithoutExtension(friendlyName);
        }

        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "app";
        }

        return baseName + ".txt";
    }
}