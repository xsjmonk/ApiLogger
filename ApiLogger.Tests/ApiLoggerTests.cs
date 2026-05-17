using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiLogger.Tests;

public class ApiLoggerTests
{
    private static string CreateTempDir()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"ApiLoggerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempPath);
        return tempPath;
    }

    [Fact]
    public void AddApiLogger_ResolvesIApiPayloadLogger()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiLogger<TestPayload>(
            options => options.LogDir = CreateTempDir(),
            new TestFormatter());

        using var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<IApiPayloadLogger<TestPayload>>();
        Assert.NotNull(logger);
    }

    [Fact]
    public void AddApiLogger_RegistersHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiLogger<TestPayload>(
            options => options.LogDir = CreateTempDir(),
            new TestFormatter());

        using var sp = services.BuildServiceProvider();
        var hostedServices = sp.GetServices<IHostedService>().ToList();
        Assert.NotEmpty(hostedServices);
        Assert.Contains(hostedServices, hs => hs is ApiLogHostedService<TestPayload>);
    }

    [Fact]
    public void AddApiLogger_DoesNotRegisterIApiLogRuntime()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiLogger<TestPayload>(
            options => options.LogDir = CreateTempDir(),
            new TestFormatter());

        using var sp = services.BuildServiceProvider();
        var runtime = sp.GetService<IApiLogRuntime<TestPayload>>();
        Assert.Null(runtime);
    }

    [Fact]
    public void AddApiLoggerRuntime_ResolvesIApiPayloadLogger()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiLoggerRuntime<TestPayload>(
            options => options.LogDir = CreateTempDir(),
            new TestFormatter());

        using var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<IApiPayloadLogger<TestPayload>>();
        Assert.NotNull(logger);
    }

    [Fact]
    public void AddApiLoggerRuntime_ResolvesIApiLogRuntime()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiLoggerRuntime<TestPayload>(
            options => options.LogDir = CreateTempDir(),
            new TestFormatter());

        using var sp = services.BuildServiceProvider();
        var runtime = sp.GetRequiredService<IApiLogRuntime<TestPayload>>();
        Assert.NotNull(runtime);
    }

    [Fact]
    public void AddApiLoggerRuntime_ResolvesIApiLogRuntimeAndSameLoggerInstance()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiLoggerRuntime<TestPayload>(
            options => options.LogDir = CreateTempDir(),
            new TestFormatter());

        using var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<IApiPayloadLogger<TestPayload>>();
        var runtime = sp.GetRequiredService<IApiLogRuntime<TestPayload>>();
        Assert.Same(runtime, logger);
    }

    [Fact]
    public void AddApiLoggerRuntime_DoesNotRegisterHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiLoggerRuntime<TestPayload>(
            options => options.LogDir = CreateTempDir(),
            new TestFormatter());

        using var sp = services.BuildServiceProvider();
        var hostedServices = sp.GetServices<IHostedService>().ToList();
        Assert.DoesNotContain(hostedServices, hs => hs is ApiLogHostedService<TestPayload>);
    }

    [Fact]
    public async Task AddApiLoggerRuntime_WritesToFile()
    {
        var tempDir = CreateTempDir();
        try
        {
var services = new ServiceCollection();
             services.AddLogging();
             services.AddApiLoggerRuntime<TestPayload>(
                 options =>
                 {
                     options.LogDir = tempDir;
                     options.LogFileName = "api_visit.log";
                 },
                 new TestFormatter());

            using var sp = services.BuildServiceProvider();
            var logger = sp.GetRequiredService<IApiPayloadLogger<TestPayload>>();

logger.Log(DateTime.Now, ApiLogKind.Info, new TestPayload("test-message"));

             var runtime = sp.GetRequiredService<IApiLogRuntime<TestPayload>>();
             await runtime.FlushAsync();

             var logFile = Path.Combine(tempDir, "api_visit.log");
             Assert.True(File.Exists(logFile));
             var content = await File.ReadAllTextAsync(logFile);
             Assert.Contains("test-message", content);
         }
         finally
         {
             Directory.Delete(tempDir, true);
         }
     }

     [Fact]
     public async Task ApiLogRuntimeFactory_CreateWritesToFile()
     {
         var tempDir = CreateTempDir();
         try
         {
             using var runtime = ApiLogRuntimeFactory.Create<TestPayload>(
                 options =>
                 {
                     options.LogDir = tempDir;
                     options.LogFileName = "api_visit.log";
                     options.RotateSize = "1MB";
                 },
                 new TestFormatter());

            runtime.Log(DateTime.Now, ApiLogKind.Info, new TestPayload("factory-message"));
            await runtime.FlushAsync();

            var logFile = Path.Combine(tempDir, "api_visit.log");
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("factory-message", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ApiLogRuntime_FlushAsyncImmediatelyAfterLog_DrainsQueueAndWritesToFile()
    {
        var tempDir = CreateTempDir();
        try
        {
using var runtime = ApiLogRuntimeFactory.Create<TestPayload>(
                 options =>
                 {
                     options.LogDir = tempDir;
                     options.LogFileName = "api_visit.log";
                 },
                 new TestFormatter());

             runtime.Log(DateTime.Now, ApiLogKind.Info, new TestPayload("immediate-flush-test"));
             await runtime.FlushAsync();

             var logFile = Path.Combine(tempDir, "api_visit.log");
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("immediate-flush-test", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task AddApiLoggerRuntime_FlushAsyncImmediatelyAfterLog_DrainsQueueAndWritesToFile()
    {
        var tempDir = CreateTempDir();
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
services.AddApiLoggerRuntime<TestPayload>(
                 options =>
                 {
                     options.LogDir = tempDir;
                     options.LogFileName = "api_visit.log";
                 },
                 new TestFormatter());

             using var sp = services.BuildServiceProvider();
             var logger = sp.GetRequiredService<IApiPayloadLogger<TestPayload>>();
             var runtime = sp.GetRequiredService<IApiLogRuntime<TestPayload>>();

             logger.Log(DateTime.Now, ApiLogKind.Info, new TestPayload("di-immediate-flush"));
            await runtime.FlushAsync();

            var logFile = Path.Combine(tempDir, "api_visit.log");
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("di-immediate-flush", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ApiLogRuntime_StopAsyncImmediatelyAfterLog_DrainsToFile()
    {
        var tempDir = CreateTempDir();
        try
        {
using var runtime = ApiLogRuntimeFactory.Create<TestPayload>(
                 options =>
                 {
                     options.LogDir = tempDir;
                     options.LogFileName = "api_visit.log";
                 },
                 new TestFormatter());

             runtime.Log(DateTime.Now, ApiLogKind.Info, new TestPayload("stop-immediate"));
            await runtime.StopAsync();

            var logFile = Path.Combine(tempDir, "api_visit.log");
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("stop-immediate", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ApiLogRuntime_FlushAfterStop_DoesNotThrow()
    {
        var tempDir = CreateTempDir();
        try
        {
using var runtime = ApiLogRuntimeFactory.Create<TestPayload>(
                 options =>
                 {
                     options.LogDir = tempDir;
                     options.LogFileName = "api_visit.log";
                 },
                 new TestFormatter());

             await runtime.StopAsync();

             var exception = await Record.ExceptionAsync(() => runtime.FlushAsync());
            Assert.Null(exception);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ApiLogRuntimeFactory_StopAsync_DrainsAndNoOpAfter()
    {
        var tempDir = CreateTempDir();
        try
        {
using var runtime = ApiLogRuntimeFactory.Create<TestPayload>(
                options =>
                {
                    options.LogDir = tempDir;
                    options.LogFileName = "api_visit.log";
                },
                new TestFormatter());

            runtime.Log(DateTime.Now, ApiLogKind.Info, new TestPayload("before-stop"));

            await runtime.StopAsync();

            runtime.Log(DateTime.Now, ApiLogKind.Info, new TestPayload("after-stop"));

            var logFile = Path.Combine(tempDir, "api_visit.log");
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("before-stop", content);
            Assert.DoesNotContain("after-stop", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ApiLogRuntime_LogAfterStop_DoesNotThrow()
    {
        var tempDir = CreateTempDir();
        try
        {
using var runtime = ApiLogRuntimeFactory.Create<TestPayload>(
                options =>
                {
                    options.LogDir = tempDir;
                    options.LogFileName = "api_visit.log";
                },
                new TestFormatter());

            runtime.Log(DateTime.Now, ApiLogKind.Info, new TestPayload("test"));

            runtime.StopAsync().GetAwaiter().GetResult();

            var exception = Record.Exception(() =>
                runtime.Log(DateTime.Now, ApiLogKind.Info, new TestPayload("after-stop")));

            Assert.Null(exception);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ApiLoggerCsproj_IsAotCompatible_AndHasTrimmingAnalyzersEnabled()
    {
        var baseDir = AppContext.BaseDirectory;
        var projectDir = baseDir;
        while (!File.Exists(Path.Combine(projectDir, "ApiLogger.csproj")) && projectDir != Path.GetPathRoot(projectDir))
        {
            projectDir = Path.GetDirectoryName(projectDir)!;
        }

        var csprojPath = Path.Combine(projectDir, "ApiLogger.csproj");

        Assert.True(File.Exists(csprojPath), $"ApiLogger.csproj not found at {csprojPath}");

        var content = File.ReadAllText(csprojPath);

        Assert.Contains("<IsAotCompatible>true</IsAotCompatible>", content);
        Assert.Contains("<EnableTrimAnalyzer>true</EnableTrimAnalyzer>", content);
        Assert.Contains("<EnableAotAnalyzer>true</EnableAotAnalyzer>", content);
    }

    [Fact]
    public void DocsExist_AndIncludeRequiredPaths()
    {
        var baseDir = AppContext.BaseDirectory;
        var projectDir = baseDir;
        while (!File.Exists(Path.Combine(projectDir, "ApiLogger.csproj")) && projectDir != Path.GetPathRoot(projectDir))
        {
            projectDir = Path.GetDirectoryName(projectDir)!;
        }

        var docsPath = Path.Combine(projectDir, "docs", "ApiLoggerUsage.md");

        Assert.True(File.Exists(docsPath), $"Docs file not found at {docsPath}");

        var content = File.ReadAllText(docsPath);

        Assert.Contains("AddApiLogger<", content);
        Assert.Contains("AddApiLoggerRuntime<", content);
        Assert.Contains("ApiLogRuntimeFactory.Create", content);
        Assert.Contains("FlushAsync", content);
        Assert.Contains("StopAsync", content);
        Assert.Contains("Dispose", content);
        Assert.Contains("AOT", content);
        Assert.Contains("Configuration from appsettings.json", content);
    }

    [Fact]
    public async Task ApiLogRuntime_NoConfiguration_WritesToAppFolder()
    {
        // Arrange - no configuration provided
        using var runtime = ApiLogRuntimeFactory.Create<ApiLogMessagePayload>();

        // Act
        runtime.Log(DateTime.UtcNow, ApiLogKind.Info, new ApiLogMessagePayload("test-message"));
        await runtime.FlushAsync();

// Assert
         var expectedFileName = Path.GetFileNameWithoutExtension(Environment.ProcessPath) + ".txt";
         if (string.IsNullOrWhiteSpace(expectedFileName) || expectedFileName == ".txt")
         {
             var friendlyName = AppDomain.CurrentDomain.FriendlyName;
             expectedFileName = string.IsNullOrWhiteSpace(friendlyName)
                 ? "app.txt"
                 : Path.GetFileNameWithoutExtension(friendlyName) + ".txt";
         }

         var logFile = Path.Combine(AppContext.BaseDirectory, expectedFileName);
         Assert.True(File.Exists(logFile), $"Expected log file not found: {logFile}");

         var content = await File.ReadAllTextAsync(logFile);
         Assert.Contains("test-message", content);

         // Ensure old hard-coded file is not created
         var oldLogFile = Path.Combine(AppContext.BaseDirectory, "api_visit.log");
         Assert.False(File.Exists(oldLogFile), "Old hard-coded api_visit.log should not be created");
    }

    [Fact]
    public async Task ApiLogRuntime_ConfiguredLogFileName_ControlsActiveAndRotatedFiles()
    {
        // Arrange
        var tempDir = CreateTempDir();
        try
        {
            using var runtime = ApiLogRuntimeFactory.Create<ApiLogMessagePayload>(
                options =>
                {
                    options.LogDir = tempDir;
                    options.LogFileName = "custom-app.txt";
                    options.RotateSize = "1KB"; // Very small to force rotation
                });

            // Act - Write enough to trigger rotation
            for (int i = 0; i < 100; i++)
            {
                runtime.Log(DateTime.UtcNow, ApiLogKind.Info, new ApiLogMessagePayload($"message-{i}"));
            }
            await runtime.FlushAsync();

// Assert
             var logFile = Path.Combine(tempDir, "custom-app.txt");
             Assert.True(File.Exists(logFile), "Active file custom-app.txt should exist");

             // Check for rotated files with pattern custom-app.*.txt
             var rotatedFiles = Directory.GetFiles(tempDir, "custom-app.*.txt");
             Assert.True(rotatedFiles.Length > 0, "Should have rotated files matching custom-app.*.txt pattern");

             // Ensure old api_visit.* files are not created
             var oldFiles = Directory.GetFiles(tempDir, "api_visit.*");
             Assert.Empty(oldFiles);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void AddApiLogger_WithConfiguration_ReadsOnlyApiLoggerNode()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("ApiLogger:LogDir", "/tmp/apilogger"),
                new KeyValuePair<string, string?>("ApiLogger:RotateSize", "5MB"),
                new KeyValuePair<string, string?>("LogDir", "/wrong/root/logdir"), // Should be ignored
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiLogger<TestPayload>(config, new TestFormatter());

        // Act
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<ApiLogOptions>();

        // Assert
        Assert.Equal("/tmp/apilogger", options.LogDir);
        Assert.Equal(5 * 1024 * 1024, options.RotateSizeBytes); // 5MB
        // LogDir from root should be ignored
    }

    [Fact]
    public async Task AddApiLoggerRuntime_WithConfiguration_WritesToConfiguredDirectory()
    {
        // Arrange
        var tempDir = CreateTempDir();
        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ApiLogger:LogDir", tempDir),
                    new KeyValuePair<string, string?>("ApiLogger:RotateSize", "1MB"),
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
services.AddApiLoggerRuntime<TestPayload>(config, new TestFormatter());

            // Act
            using var sp = services.BuildServiceProvider();
            var logger = sp.GetRequiredService<IApiPayloadLogger<TestPayload>>();
            var runtime = sp.GetRequiredService<IApiLogRuntime<TestPayload>>();

            logger.Log(DateTime.UtcNow, ApiLogKind.Info, new TestPayload("config-test"));
            await runtime.FlushAsync();

            // Assert
            var expectedFileName = Path.GetFileNameWithoutExtension(Environment.ProcessPath) + ".txt";
            if (string.IsNullOrWhiteSpace(expectedFileName) || expectedFileName == ".txt")
            {
                var friendlyName = AppDomain.CurrentDomain.FriendlyName;
                expectedFileName = string.IsNullOrWhiteSpace(friendlyName)
                    ? "app.txt"
                    : Path.GetFileNameWithoutExtension(friendlyName) + ".txt";
            }

            var logFile = Path.Combine(tempDir, expectedFileName);
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("config-test", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void AddApiLoggerRuntime_WithConfiguration_ResolvesReadyMadeInterfaces()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("ApiLogger:LogDir", "/tmp/apilogger"),
                new KeyValuePair<string, string?>("ApiLogger:RotateSize", "1MB"),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiLoggerRuntime(config); // Non-generic ready-made

        // Act
        using var sp = services.BuildServiceProvider();

// Assert
         var logger = sp.GetService<IApiLogger>();
         Assert.NotNull(logger);

         var payloadLogger = sp.GetService<IApiPayloadLogger<ApiLogMessagePayload>>();
         Assert.NotNull(payloadLogger);

         var runtime = sp.GetService<IApiLogRuntime<ApiLogMessagePayload>>();
         Assert.NotNull(runtime);

        // payloadLogger and Runtime should be the same instance (singleton)
        Assert.Same(payloadLogger, runtime);
    }

[Fact]
     public async Task ReadyMadeApiLogger_WritesInfoWarningErrorLines()
     {
         // Arrange
         var tempDir = CreateTempDir();
         try
         {
             var services = new ServiceCollection();
             services.AddLogging();
             services.AddApiLoggerRuntime(
                 options =>
                 {
                     options.LogDir = tempDir;
                     options.LogFileName = "app.txt";
                 }); // Uses explicit config

             // Act
             using var sp = services.BuildServiceProvider();
             var logger = sp.GetRequiredService<IApiLogger>();
             var runtime = sp.GetRequiredService<IApiLogRuntime<ApiLogMessagePayload>>();

             logger.LogInfo("info-message");
             logger.LogWarning("warning-message");
             logger.LogError("error-message");
             logger.LogError("exception-message", new InvalidOperationException("boom"));

             await runtime.FlushAsync();

             // Assert
             var logFile = Path.Combine(tempDir, "app.txt");
             Assert.True(File.Exists(logFile));
             var content = await File.ReadAllTextAsync(logFile);

             Assert.Contains("Info\tinfo-message", content);
             Assert.Contains("Warning\twarning-message", content);
             Assert.Contains("Error\terror-message", content);
             Assert.Contains("Error\texception-message\tSystem.InvalidOperationException\tboom", content);
         }
         finally
         {
             Directory.Delete(tempDir, true);
         }
     }

    [Fact]
    public void DefaultFormatter_SanitizesNewlinesCarriageReturnsTabs()
    {
        // Arrange
        var formatter = new DefaultApiLogMessageFormatter();
        var item = new ApiLogItem<ApiLogMessagePayload>(
            DateTime.UtcNow,
            ApiLogKind.Info,
            new ApiLogMessagePayload("line1\nline2\r\n\twith\ttabs\r", null));

        // Act
        var result = formatter.Format(item);

        // Assert - split on \t to check the message field (3rd field) specifically
        var fields = result.Split('\t');
        Assert.Equal(6, fields.Length);
        Assert.Equal("line1 line2   with tabs ", fields[2]);
        Assert.DoesNotContain("\n", fields[2]);
        Assert.DoesNotContain("\r", fields[2]);
        Assert.DoesNotContain("\t", fields[2]);
    }

    [Fact]
    public async Task CustomFormatter_WinsForReadyMadeOverload()
    {
        // Arrange
        var tempDir = CreateTempDir();
        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ApiLogger:LogDir", tempDir),
                    new KeyValuePair<string, string?>("ApiLogger:LogFileName", "custom-output.txt"),
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            // TestMessageFormatter outputs only 3 fields (timestamp, kind, message)
            // DefaultApiLogMessageFormatter outputs 6 fields (adds exception fields)
            services.AddApiLoggerRuntime(config, new TestMessageFormatter());

            // Act
            using var sp = services.BuildServiceProvider();
            var logger = sp.GetRequiredService<IApiLogger>();
            var runtime = sp.GetRequiredService<IApiLogRuntime<ApiLogMessagePayload>>();

            logger.LogInfo("test-message");

            await runtime.FlushAsync();

            // Assert
            var logFile = Path.Combine(tempDir, "custom-output.txt");
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);

            // Custom formatter produces 3 tab-separated fields (no exception fields)
            Assert.Contains("Info\ttest-message", content);
            // Default formatter would add \t\t\t (3 empty trailing exception fields)
            Assert.DoesNotContain("\t\t\t", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void MicrosoftLoggingAdapter_MapsWarningCorrectly()
    {
        // Arrange - capture the payload to avoid any queue/runtime
        var capturedKind = ApiLogKind.Info;
        var capturedMessage = string.Empty;
        IApiPayloadLogger<TestPayload> captureLogger = new TestWarningCaptureLogger(item =>
        {
            capturedKind = item.Kind;
            capturedMessage = item.Payload.Message;
        });

        var provider = new ApiLoggerProvider<TestPayload>(captureLogger, new TestPayloadFactory());
        var logger = provider.CreateLogger("TestCategory");

        // Act
        logger.LogWarning("This is a warning");

        // Assert
        Assert.Equal(ApiLogKind.Warning, capturedKind);
        Assert.Equal("This is a warning", capturedMessage);
    }

    [Fact]
    public async Task AddApiLoggerRuntime_GenericNoFormatter_WritesWithDefaultFormatter()
    {
        var tempDir = CreateTempDir();
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApiLoggerRuntime<ToStringPayload>(
                options =>
                {
                    options.LogDir = tempDir;
                    options.LogFileName = "generic-default.txt";
                });

            using var sp = services.BuildServiceProvider();
            var logger = sp.GetRequiredService<IApiPayloadLogger<ToStringPayload>>();
            var runtime = sp.GetRequiredService<IApiLogRuntime<ToStringPayload>>();

            logger.Log(DateTime.Now, ApiLogKind.Info, new ToStringPayload("abc 123"));
            await runtime.FlushAsync();

            var logFile = Path.Combine(tempDir, "generic-default.txt");
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("Info\tpayload:abc 123", content);
            Assert.DoesNotContain("abc\n123", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task RuntimeFactory_GenericNoFormatter_WritesWithDefaultFormatter()
    {
        var tempDir = CreateTempDir();
        try
        {
            using var runtime = ApiLogRuntimeFactory.Create<ToStringPayload>(
                options =>
                {
                    options.LogDir = tempDir;
                    options.LogFileName = "factory-generic-default.txt";
                });

            runtime.Log(DateTime.Now, ApiLogKind.Info, new ToStringPayload("abc 123"));
            await runtime.FlushAsync();

            var logFile = Path.Combine(tempDir, "factory-generic-default.txt");
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("Info\tpayload:abc 123", content);
            Assert.DoesNotContain("abc\n123", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void AddApiLogger_GenericNoFormatter_RegistersDefaultFormatterAndResolvesLogger()
    {
        var tempDir = CreateTempDir();
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApiLogger<TestPayload>(
                options =>
                {
                    options.LogDir = tempDir;
                    options.LogFileName = "resolution-test.txt";
                });

            using var sp = services.BuildServiceProvider();
            var formatter = sp.GetRequiredService<IApiLogFormatter<TestPayload>>();
            Assert.IsType<DefaultApiLogFormatter<TestPayload>>(formatter);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void AddApiLogger_GenericNoFormatter_WithConfiguration_ResolvesLoggerFormatterAndHostedService()
    {
        var tempDir = CreateTempDir();
        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ApiLogger:LogDir", tempDir),
                    new KeyValuePair<string, string?>("ApiLogger:LogFileName", "generic-config-default.txt"),
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApiLogger<TestPayload>(config);

            using var sp = services.BuildServiceProvider();
            var logger = sp.GetRequiredService<IApiPayloadLogger<TestPayload>>();
            Assert.NotNull(logger);

            var formatter = sp.GetRequiredService<IApiLogFormatter<TestPayload>>();
            Assert.IsType<DefaultApiLogFormatter<TestPayload>>(formatter);

            var hostedServices = sp.GetServices<IHostedService>().ToList();
            Assert.Single(hostedServices);
            Assert.Contains(hostedServices, hs => hs is ApiLogHostedService<TestPayload>);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task AddApiLoggerRuntime_GenericNoFormatter_WithConfiguration_WritesPayload()
    {
        var tempDir = CreateTempDir();
        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ApiLogger:LogDir", tempDir),
                    new KeyValuePair<string, string?>("ApiLogger:LogFileName", "runtime-generic-config-default.txt"),
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApiLoggerRuntime<TestPayload>(config);

            using var sp = services.BuildServiceProvider();
            var logger = sp.GetRequiredService<IApiPayloadLogger<TestPayload>>();
            var runtime = sp.GetRequiredService<IApiLogRuntime<TestPayload>>();

            logger.Log(DateTime.Now, ApiLogKind.Info, new TestPayload("runtime-config-default-test"));
            await runtime.FlushAsync();

            var logFile = Path.Combine(tempDir, "runtime-generic-config-default.txt");
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("runtime-config-default-test", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task RuntimeFactory_GenericNoFormatter_WithConfiguration_WritesPayload()
    {
        var tempDir = CreateTempDir();
        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ApiLogger:LogDir", tempDir),
                    new KeyValuePair<string, string?>("ApiLogger:LogFileName", "factory-generic-config-default.txt"),
                })
                .Build();

            using var runtime = ApiLogRuntimeFactory.Create<TestPayload>(config);

            runtime.Log(DateTime.Now, ApiLogKind.Info, new TestPayload("factory-config-default-test"));
            await runtime.FlushAsync();

            var logFile = Path.Combine(tempDir, "factory-generic-config-default.txt");
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("factory-config-default-test", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void AddApiLogger_GenericNoFormatter_ResolvesHostedServicesWithoutMissingFormatter()
    {
        var tempDir = CreateTempDir();
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApiLogger<TestPayload>(
                options =>
                {
                    options.LogDir = tempDir;
                    options.LogFileName = "hosted-generic-default.txt";
                });

            using var sp = services.BuildServiceProvider();
            var hostedServices = sp.GetServices<IHostedService>().ToList();
            Assert.Single(hostedServices);
            Assert.Contains(hostedServices, hs => hs is ApiLogHostedService<TestPayload>);

            var logger = sp.GetRequiredService<IApiPayloadLogger<TestPayload>>();
            Assert.NotNull(logger);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task AddApiLoggerRuntime_GenericCustomFormatter_WinsOverDefaultFormatter()
    {
        var tempDir = CreateTempDir();
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApiLoggerRuntime<TestPayload>(
                options =>
                {
                    options.LogDir = tempDir;
                    options.LogFileName = "custom-default-test.txt";
                },
                new TestFormatter());

            using var sp = services.BuildServiceProvider();
            var logger = sp.GetRequiredService<IApiPayloadLogger<TestPayload>>();
            var runtime = sp.GetRequiredService<IApiLogRuntime<TestPayload>>();

            logger.Log(DateTime.Now, ApiLogKind.Info, new TestPayload("custom-formatter-wins"));
            await runtime.FlushAsync();

            var logFile = Path.Combine(tempDir, "custom-default-test.txt");
            Assert.True(File.Exists(logFile));
            var content = await File.ReadAllTextAsync(logFile);
            Assert.Contains("Info\tcustom-formatter-wins", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void DefaultApiLogFormatter_SanitizesPayloadToString()
    {
        // Arrange - a payload whose ToString() returns \r\n\t
        var payload = new PayloadWithSpecialToString();
        var formatter = new DefaultApiLogFormatter<PayloadWithSpecialToString>();
        var item = new ApiLogItem<PayloadWithSpecialToString>(
            DateTime.UtcNow,
            ApiLogKind.Info,
            payload);

        // Act
        var result = formatter.Format(item);

        // Assert - split on \t to check the payload field (3rd field) specifically
        var fields = result.Split('\t');
        Assert.Equal(3, fields.Length);
        Assert.DoesNotContain("\n", fields[2]);
        Assert.DoesNotContain("\r", fields[2]);
        Assert.DoesNotContain("\t", fields[2]);
        Assert.Equal("line1  line2 end", fields[2]);
    }

    [Fact]
    public void SourceFiles_HaveNoForbiddenPatterns()
    {
        var baseDir = AppContext.BaseDirectory;
        var projectDir = baseDir;
        while (!File.Exists(Path.Combine(projectDir, "ApiLogger.csproj")) && projectDir != Path.GetPathRoot(projectDir))
        {
            projectDir = Path.GetDirectoryName(projectDir)!;
        }

        var srcDir = Path.Combine(projectDir, "src");
        Assert.True(Directory.Exists(srcDir), $"src directory not found at {srcDir}");

        var forbidden = new[]
        {
            ".Bind(",
            ".Get<",
            "ConfigurationBinder",
            "System.Text.Json",
            "Newtonsoft.Json",
            "PropertyInfo",
            "GetProperties(",
            "MakeGenericType",
            "Activator.CreateInstance",
            "dynamic ",
            "RequiresUnreferencedCode",
            "RequiresDynamicCode",
        };

        var failures = new List<string>();
        foreach (var file in Directory.EnumerateFiles(srcDir, "*.cs"))
        {
            var content = File.ReadAllText(file);
            foreach (var pattern in forbidden)
            {
                if (content.Contains(pattern, StringComparison.Ordinal))
                {
                    failures.Add($"{Path.GetFileName(file)} contains forbidden pattern: {pattern}");
                }
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public void Docs_UseGenericNamesAndDoNotRecommendVisitNames()
    {
        var baseDir = AppContext.BaseDirectory;
        var projectDir = baseDir;
        while (!File.Exists(Path.Combine(projectDir, "ApiLogger.csproj")) && projectDir != Path.GetPathRoot(projectDir))
        {
            projectDir = Path.GetDirectoryName(projectDir)!;
        }

        var docsPath = Path.Combine(projectDir, "docs", "ApiLoggerUsage.md");
        Assert.True(File.Exists(docsPath));

        var content = File.ReadAllText(docsPath);

        Assert.Contains("IApiPayloadLogger<", content);
        Assert.Contains("ApiLogItem<", content);
        Assert.Contains("ApiLogKind.Info", content);
        Assert.Contains("IApiLogFormatter<", content);
        Assert.Contains("ApiLogRuntimeFactory.Create", content);

        Assert.DoesNotContain("IApiVisitLogger<", content);
        Assert.DoesNotContain("ApiVisitLogItem", content);
        Assert.DoesNotContain("ApiVisitLogKind", content);
        Assert.DoesNotContain("IApiVisitLogFormatter<", content);
        Assert.DoesNotContain("ApiVisitLogRuntimeFactory.Create", content);
    }

    [Fact]
    public void SourceFiles_DoNotUseOldVisitNamesInPrimaryImplementation()
    {
        var baseDir = AppContext.BaseDirectory;
        var projectDir = baseDir;
        while (!File.Exists(Path.Combine(projectDir, "ApiLogger.csproj")) && projectDir != Path.GetPathRoot(projectDir))
        {
            projectDir = Path.GetDirectoryName(projectDir)!;
        }

        var srcDir = Path.Combine(projectDir, "src");
        Assert.True(Directory.Exists(srcDir));

        var primaryFiles = new[]
        {
            "ApiLogger.cs",
            "ApiLoggerServiceCollectionExtensions.cs",
            "ApiLogRuntime.cs",
            "ApiLogWriter.cs",
            "ApiLogHostedService.cs",
            "ApiPayloadLogger.cs",
            "DefaultApiLogFormatter.cs",
            "ApiLoggerModels.cs",
            "ApiLogRuntimeFactory.cs",
            "ApiLogQueue.cs",
            "ApiLoggerAdapter.cs",
            "ApiLoggerProvider.cs",
        };

        var forbidden = new[] { "ApiVisit", "VisitLog" };

        var failures = new List<string>();
        foreach (var file in primaryFiles)
        {
            var path = Path.Combine(srcDir, file);
            if (!File.Exists(path)) continue;

            var content = File.ReadAllText(path);
            foreach (var pattern in forbidden)
            {
                if (content.Contains(pattern, StringComparison.Ordinal))
                {
                    failures.Add($"{file} contains old name: {pattern}");
                }
            }
        }

        Assert.Empty(failures);
    }
}

public sealed class PayloadWithSpecialToString
{
    public override string ToString() => "line1\r\nline2\tend";
}