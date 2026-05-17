# ApiLogger Usage Guide

ApiLogger is a high-performance, AOT-friendly logging library for .NET that supports both ASP.NET Core / Generic Host applications and plain console applications.

## Configuration from appsettings.json

ApiLogger can be configured through the standard Microsoft.Extensions.Configuration system by binding to the "ApiLogger" section.

```json
{
  "ApiLogger": {
    "LogDir": "C:\\FunTool\\logs\\api",
    "RotateSize": "10MB",
    "LogFileName": "my_app.txt"
  }
}
```

The configuration system must be set up by the host application (e.g., via `builder.Configuration` in ASP.NET Core or `new ConfigurationBuilder().AddJsonFile("appsettings.json")` in console apps). ApiLogger does not load JSON files directly.

## Quick start: ILogger without DI

For a simple console application without dependency injection:

```csharp
// Build configuration (optional — without it, logs go to AppContext.BaseDirectory)
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// Create a logger handle — dispose it to drain and flush queued items
await using var handle = ApiLoggerFactory.CreateLogger(configuration);
var logger = handle.Logger;

logger.LogInformation("Application started");
logger.LogWarning("High memory usage detected");

try
{
    // Some operation that might fail
}
catch (Exception ex)
{
    logger.LogError(ex, "Operation failed");
}

// On dispose, queued items are drained and written to disk automatically.
// No need to resolve, flush, or stop ApiLogRuntime.
```

Additional overloads:

```csharp
// No configuration (defaults)
await using var handle = ApiLoggerFactory.CreateLogger();

// From configure action
await using var handle = ApiLoggerFactory.CreateLogger(options =>
{
    options.LogDir = "./logs";
    options.RotateSize = "10MB";
});

// With explicit ApiLogOptions
await using var handle = ApiLoggerFactory.CreateLogger(new ApiLogOptions { LogFileName = "app.txt" });

// With category name
await using var handle = ApiLoggerFactory.CreateLogger("MyCategory");

// With typed category
await using var handle = ApiLoggerFactory.CreateLogger<MyService>();
```

## Quick start: ILogger with DI (no Generic Host)

For a console application using a plain DI container:

```csharp
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddApiLogger(configuration));

await using var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Application started");

// On provider dispose, the ApiLogRuntime singleton is disposed,
// which drains and flushes all queued items. No need to resolve
// or manage ApiLogRuntime.
```

Additional overloads:

```csharp
builder.AddApiLogger();                           // No configuration (defaults)
builder.AddApiLogger(configuration);              // From IConfiguration
builder.AddApiLogger(options => { ... });         // From configure action
builder.AddApiLogger(new ApiLogOptions { ... });  // From ApiLogOptions instance
```

## Generic Host / ASP.NET Core registration

When using a Generic Host or ASP.NET Core, use `AddApiLogger()`:

```csharp
builder.Services.AddApiLogger(builder.Configuration);
```

Or with a custom formatter:

```csharp
builder.Services.AddApiLogger(
    builder.Configuration,
    new MyApiLogMessageFormatter());
```

The host starts and stops a hosted service that manages background queue draining. On host stop, queued items are drained automatically.

For ready-made string-based logging with `IApiLogger`:

```csharp
var logger = host.Services.GetRequiredService<IApiLogger>();
logger.LogInfo("Application started");
```

## Advanced: custom payload with Generic Host

For structured payloads, define a payload type and formatter, then use the generic `AddApiLogger<TPayload>()`:

```csharp
public sealed class MyPayload
{
    public MyPayload(string message) => Message = message;
    public string Message { get; }
}

public sealed class MyFormatter : IApiLogFormatter<MyPayload>
{
    public string Format(ApiLogItem<MyPayload> item) =>
        string.Join('\t',
            item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            item.Kind,
            item.Payload.Message);
}

builder.Services.AddApiLogger<MyPayload>(
    options =>
    {
        options.LogDir = "./logs";
        options.RotateSize = "10MB";
    },
    new MyFormatter());
```

Then inject `IApiPayloadLogger<MyPayload>`:

```csharp
var payloadLogger = serviceProvider.GetRequiredService<IApiPayloadLogger<MyPayload>>();
payloadLogger.Log(DateTime.Now, ApiLogKind.Info, new MyPayload("page-view"));
```

## Advanced: console without DI (custom payload)

For simple console apps without DI that need custom payloads, use `ApiLogRuntimeFactory`:

```csharp
using var runtime = ApiLogRuntimeFactory.Create<MyPayload>(
    options =>
    {
        options.LogDir = "./logs";
        options.RotateSize = "10MB";
    },
    new MyFormatter());

runtime.Log(DateTime.Now, ApiLogKind.Info, new MyPayload("console-started"));
await runtime.FlushAsync();
```

Ready-made shortcut for `ApiLogMessagePayload`:

```csharp
using var runtime = ApiLogRuntimeFactory.Create();
runtime.Log(DateTime.Now, ApiLogKind.Info, new ApiLogMessagePayload("hello"));
await runtime.FlushAsync();
```

## Advanced: console DI without Host (custom payload)

For console apps with DI but no Generic Host:

```csharp
services.AddApiLoggerRuntime<MyPayload>(
    options =>
    {
        options.LogDir = "./logs";
        options.RotateSize = "10MB";
    },
    new MyFormatter());

await using var provider = services.BuildServiceProvider();
var payloadLogger = provider.GetRequiredService<IApiPayloadLogger<MyPayload>>();
payloadLogger.Log(DateTime.Now, ApiLogKind.Info, new MyPayload("job-started"));
// Dispose provider to drain
```

## Shutdown: FlushAsync, StopAsync, Dispose

For the ready-made `ILogger` paths, disposing the handle or the DI service provider drains and flushes automatically.

For advanced `IApiLogRuntime` usage, you may need explicit lifecycle management:

- `FlushAsync()`: Ensures all log items accepted before the call are written and flushed to disk.
- `StopAsync()`: Stops the runtime, drains queued items, flushes the writer, and disposes resources.
- `Dispose()`: Calls `StopAsync()` internally; use it for simple cleanup.

## AOT Compatibility

The logger is AOT-compatible. No reflection-based serialization is used. Payload formatting is caller-provided through `IApiLogFormatter<TPayload>`. For AOT apps that format JSON, use source-generated serializers (e.g., System.Text.Json source generators).

## Ready-made Logger (IApiLogger)

For simple string-based logging, the ready-made `IApiLogger` interface is available through DI:

```csharp
var logger = provider.GetRequiredService<IApiLogger>();
logger.LogInfo("Application started");
logger.LogWarning("High memory usage detected");
try
{
    // Some operation that might fail
}
catch (Exception ex)
{
    logger.LogError("Operation failed", ex);
}
```

## Default Formatter Rules

When no custom formatter is supplied, ApiLogger selects the default formatter based on the payload type:

- **`ApiLogMessagePayload`** (ready-made path) uses `DefaultApiLogMessageFormatter`, which writes 6 tab-separated fields: timestamp, kind, message, exception type, exception message, and exception stack trace.
- **Custom payload types** (`TPayload`) use `DefaultApiLogFormatter<TPayload>`, which writes 3 tab-separated fields: timestamp, kind, and `payload.ToString()`. This formatter does not inspect object properties and is fully AOT-safe. For structured or multi-field payloads, provide a custom `IApiLogFormatter<TPayload>`.

## Default Formatter Comparison

| Scenario | Formatter | Output Fields |
|---|---|---|
| `AddApiLogger()` ready-made | `DefaultApiLogMessageFormatter` | timestamp, kind, message, exceptionType, exceptionMessage, exceptionStackTrace |
| `AddApiLogger<MyPayload>()` generic, no custom formatter | `DefaultApiLogFormatter<MyPayload>` | timestamp, kind, `payload.ToString()` |

Custom formatters always override the default. If you register a custom `IApiLogFormatter<TPayload>`, it is used for all log entries regardless of payload type.

## Important Notes

- For normal `ILogger` usage: dispose the `ApiLoggerHandle` or the DI service provider. Do not resolve or manage `ApiLogRuntime`.
- Use `AddApiLogger<TPayload>` when the app uses a Generic Host / ASP.NET Core host because the host starts/stops the hosted service.
- Use `AddApiLoggerRuntime<TPayload>` or `ApiLogRuntimeFactory.Create(...)` when there is no host service and you need custom payload support.
- Dispose the runtime or DI service provider before process exit to drain queued items.
- The logger is AOT/trimming friendly. No reflection-based serialization, configuration binding, or property inspection is used in production code.
- Logging calls are fast and non-blocking; log items are enqueued and a background drain writes to disk.
- Failures in logging must not crash application code. All logging operations are resilient.
- Generic payload APIs (`IApiPayloadLogger<TPayload>`) remain available for advanced/custom payload logging.
- Ready-made `IApiLogger` is the simple API when the consumer only needs info/warning/error string logging.
- The default formatter is used only when no custom formatter is supplied. For `ApiLogMessagePayload`, the richer `DefaultApiLogMessageFormatter` is used. For generic payloads, `DefaultApiLogFormatter<TPayload>` logs `payload.ToString()` only.
- ApiLogger reads from the `ApiLogger` configuration node and does not load JSON files by itself.
- When no configuration/options are provided, ApiLogger writes to the current running app folder and names the active log file after the running app, for example `my_app.exe` -> `my_app.txt`.
- `ApiLogger:LogFileName` or `ApiLogOptions.LogFileName` can override the file name; `ApiLogger:LogDir` or `ApiLogOptions.LogDir` can override the folder.
- Provide a custom `IApiLogFormatter<TPayload>` for structured payloads with multiple fields; the default generic formatter only calls `ToString()`.
