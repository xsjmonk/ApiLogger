using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiLogger.Tests;

public sealed class ApiLoggerUsageTest
{
    [Fact]
    public async Task NoDependencyInjection_WithoutConfiguration()
    {
        using var runtime = ApiLogRuntimeFactory.Create();
        runtime.Log(DateTime.UtcNow, ApiLogKind.Info, new ApiLogMessagePayload("standalone-no-config"));
        await runtime.FlushAsync();

        var expectedFile = Path.GetFileNameWithoutExtension(Environment.ProcessPath) + ".txt";
        var logFile = Path.Combine(AppContext.BaseDirectory, expectedFile);
        Assert.True(File.Exists(logFile));
    }

    [Fact]
    public async Task NoDependencyInjection_WithConfiguration()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ApiLoggerTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiLogger:LogDir"] = tempDir,
                    ["ApiLogger:LogFileName"] = "standalone-config.txt",
                    ["ApiLogger:RotateSize"] = "1MB",
                })
                .Build();

            using var runtime = ApiLogRuntimeFactory.Create(configuration);
            runtime.Log(DateTime.UtcNow, ApiLogKind.Info, new ApiLogMessagePayload("standalone-with-config"));
            await runtime.FlushAsync();

            var logFile = Path.Combine(tempDir, "standalone-config.txt");
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("standalone-with-config", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DependencyInjection_WithoutConfiguration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiLoggerRuntime();

        await using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<IApiLogger>();
        logger.LogInfo("di-no-config");

        var runtime = provider.GetRequiredService<IApiLogRuntime<ApiLogMessagePayload>>();
        await runtime.FlushAsync();
    }

    [Fact]
    public async Task DependencyInjection_WithConfiguration()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ApiLoggerTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiLogger:LogDir"] = tempDir,
                    ["ApiLogger:LogFileName"] = "di-config.txt",
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApiLoggerRuntime(configuration);

            await using var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<IApiLogger>();
            logger.LogInfo("di-with-config");

            var runtime = provider.GetRequiredService<IApiLogRuntime<ApiLogMessagePayload>>();
            await runtime.FlushAsync();

            var logFile = Path.Combine(tempDir, "di-config.txt");
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("di-with-config", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GenericHost_WithConfiguration()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ApiLoggerTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration(config =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ApiLogger:LogDir"] = tempDir,
                        ["ApiLogger:LogFileName"] = "host-config.txt",
                    }))
                .ConfigureLogging(logging => logging.ClearProviders())
                .ConfigureServices((context, services) =>
                {
                    services.AddApiLogger(context.Configuration);
                });

            using var host = hostBuilder.Build();
            await host.StartAsync();

            var logger = host.Services.GetRequiredService<IApiLogger>();
            logger.LogInfo("host-with-config");

            await host.StopAsync();

            var logFile = Path.Combine(tempDir, "host-config.txt");
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("host-with-config", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ApiLoggerFactory_CreateLogger()
    {
        var expectedFile = Path.GetFileNameWithoutExtension(Environment.ProcessPath) + ".txt";
        var logFile = Path.Combine(AppContext.BaseDirectory, expectedFile);
        {
            await using var handle = ApiLoggerFactory.CreateLogger();
            handle.Logger.LogInformation("factory-no-category");
        }
        Assert.True(File.Exists(logFile));
    }

    [Fact]
    public async Task ApiLoggerFactory_CreateLogger_WithConfiguration()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ApiLoggerTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiLogger:LogDir"] = tempDir,
                    ["ApiLogger:LogFileName"] = "factory-config.txt",
                })
                .Build();

            var logFile = Path.Combine(tempDir, "factory-config.txt");
            {
                await using var handle = ApiLoggerFactory.CreateLogger(configuration);
                handle.Logger.LogInformation("factory-with-config");
            }
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("factory-with-config", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ApiLoggerLoggingBuilderExtensions_AddApiLogger()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ApiLoggerTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiLogger:LogDir"] = tempDir,
                    ["ApiLogger:LogFileName"] = "logging-builder.txt",
                })
                .Build();

            var logFile = Path.Combine(tempDir, "logging-builder.txt");
            {
                var services = new ServiceCollection();
                services.AddLogging(builder => builder.AddApiLogger(configuration));

                await using var provider = services.BuildServiceProvider();
                var logger = provider.GetRequiredService<ILogger<ApiLoggerUsageTest>>();
                logger.LogInformation("from-logging-builder");
            }
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("from-logging-builder", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ApiLoggerLoggingBuilderExtensions_AddApiLogger_NoConfig()
    {
        var expectedFile = Path.GetFileNameWithoutExtension(Environment.ProcessPath) + ".txt";
        var logFile = Path.Combine(AppContext.BaseDirectory, expectedFile);
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddApiLogger());

            await using var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<ApiLoggerUsageTest>>();
            logger.LogInformation("logging-builder-no-config");
        }
        Assert.True(File.Exists(logFile));
    }

    [Fact]
    public async Task LoggerWithTag_WritesToTaggedLogFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ApiLoggerTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiLogger:LogDir"] = tempDir,
                    ["ApiLogger:LogFileName"] = "app.txt",
                    ["ApiLogger:RotateSize"] = "10MB",
                })
                .Build();

            var handle = ApiLoggerFactory.CreateLogger(configuration);
            try
            {
                handle.Logger.LogWithTag(
                    logLevel: LogLevel.Information,
                    tag: "my_tag",
                    message: "tag-message");

                await handle.FlushAsync();
            }
            finally
            {
                await handle.DisposeAsync();
            }

            var taggedLogFile = Path.Combine(tempDir, "app.my_tag.txt");
            Assert.True(File.Exists(taggedLogFile), "Tagged log file should exist.");

            var content = await File.ReadAllTextAsync(taggedLogFile);
            Assert.Contains("tag-message", content);

            var normalLogFile = Path.Combine(tempDir, "app.txt");
            Assert.False(File.Exists(normalLogFile), "Normal log file should not be created for tagged-only write.");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task Console_NoDependencyInjection_ILogger_DisposeHandle_DrainsWithoutRuntimeAccess()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ApiLoggerTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiLogger:LogDir"] = tempDir,
                    ["ApiLogger:LogFileName"] = "console-logger-no-runtime.txt",
                })
                .Build();

            var logFile = Path.Combine(tempDir, "console-logger-no-runtime.txt");
            {
                await using var handle = ApiLoggerFactory.CreateLogger(configuration);
                var logger = handle.Logger;
                logger.LogInformation("console-logger-no-runtime");
            }
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("console-logger-no-runtime", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task Console_DependencyInjection_ILogger_DisposeProvider_DrainsWithoutRuntimeAccess()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ApiLoggerTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiLogger:LogDir"] = tempDir,
                    ["ApiLogger:LogFileName"] = "di-logger-no-runtime.txt",
                })
                .Build();

            var logFile = Path.Combine(tempDir, "di-logger-no-runtime.txt");
            {
                var services = new ServiceCollection();
                services.AddLogging(builder => builder.AddApiLogger(configuration));

                await using var provider = services.BuildServiceProvider();
                var logger = provider.GetRequiredService<ILogger<ApiLoggerUsageTest>>();
                logger.LogInformation("di-logger-no-runtime");
            }
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("di-logger-no-runtime", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ApiLoggerHandle_StopAsyncThenDisposeAsync_DisposesOwnedFactoryAndIsIdempotent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ApiLoggerTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiLogger:LogDir"] = tempDir,
                    ["ApiLogger:LogFileName"] = "stop-dispose.txt",
                })
                .Build();

            var logFile = Path.Combine(tempDir, "stop-dispose.txt");
            {
                await using var handle = ApiLoggerFactory.CreateLogger(configuration);
                handle.Logger.LogInformation("stop-dispose-test");
                await handle.StopAsync();
                await handle.DisposeAsync();
                await handle.DisposeAsync();
            }
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("stop-dispose-test", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task Docs_EasyUsage_DoesNotRecommendRuntimeManagement()
    {
        var content = await File.ReadAllTextAsync(FindDocsFile());

        var builderSection = ExtractSection(content, "## ILogger integration with ILoggingBuilder");
        Assert.DoesNotContain("IApiLogRuntime", builderSection);
        Assert.DoesNotContain("ApiLogRuntimeFactory.Create", builderSection);
        Assert.DoesNotContain("await runtime.FlushAsync", builderSection);

        var factorySection = ExtractSection(content, "## Standalone ILogger with ApiLoggerFactory (non-DI)");
        Assert.DoesNotContain("IApiLogRuntime", factorySection);
        Assert.DoesNotContain("ApiLogRuntimeFactory.Create", factorySection);
        Assert.DoesNotContain("await runtime.FlushAsync", factorySection);
    }

    private static string FindDocsFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "docs", "ApiLoggerUsage.md");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Could not find docs/ApiLoggerUsage.md");
    }

    private static string ExtractSection(string markdown, string sectionHeader)
    {
        var start = markdown.IndexOf(sectionHeader, StringComparison.Ordinal);
        if (start < 0)
            return string.Empty;

        start += sectionHeader.Length;
        var nextSection = markdown.IndexOf("## ", start, StringComparison.Ordinal);
        var length = nextSection > 0 ? nextSection - start : markdown.Length - start;
        return markdown.Substring(start, length);
    }
}
