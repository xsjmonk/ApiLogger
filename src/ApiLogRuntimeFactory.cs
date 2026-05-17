using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ApiLogger;

public static class ApiLogRuntimeFactory
{
    private static IApiLogFormatter<T> ResolveDefaultFormatter<T>()
    {
        if (typeof(T) == typeof(ApiLogMessagePayload))
        {
            return (IApiLogFormatter<T>)(object)new DefaultApiLogMessageFormatter();
        }
        return new DefaultApiLogFormatter<T>();
    }

    // Generic typed payload factory methods

    public static IApiLogRuntime<TPayload> Create<TPayload>(
        Action<ApiLogOptions> configure,
        IApiLogFormatter<TPayload> formatter,
        ILoggerFactory? loggerFactory = null)
    {
        var updatedOptions = ApiLogOptionsFactory.CreateNormalized(configure);
        return new ApiLogRuntime<TPayload>(updatedOptions, formatter, loggerFactory);
    }

    public static IApiLogRuntime<TPayload> Create<TPayload>(
        ApiLogOptions options,
        IApiLogFormatter<TPayload> formatter,
        ILoggerFactory? loggerFactory = null)
    {
        var updated = ApiLogOptionsFactory.CreateNormalized(options);
        return new ApiLogRuntime<TPayload>(updated, formatter, loggerFactory);
    }

    public static IApiLogRuntime<TPayload> Create<TPayload>(
        ILoggerFactory? loggerFactory = null)
    {
        var updatedOptions = ApiLogOptionsFactory.CreateDefault();
        var formatter = ResolveDefaultFormatter<TPayload>();
        return new ApiLogRuntime<TPayload>(updatedOptions, formatter, loggerFactory);
    }

    public static IApiLogRuntime<TPayload> Create<TPayload>(
        IApiLogFormatter<TPayload> formatter,
        ILoggerFactory? loggerFactory = null)
    {
        var updatedOptions = ApiLogOptionsFactory.CreateDefault();
        return new ApiLogRuntime<TPayload>(updatedOptions, formatter, loggerFactory);
    }

    public static IApiLogRuntime<TPayload> Create<TPayload>(
        Action<ApiLogOptions> configure,
        ILoggerFactory? loggerFactory = null)
    {
        var updatedOptions = ApiLogOptionsFactory.CreateNormalized(configure);
        var formatter = ResolveDefaultFormatter<TPayload>();
        return new ApiLogRuntime<TPayload>(updatedOptions, formatter, loggerFactory);
    }

    public static IApiLogRuntime<TPayload> Create<TPayload>(
        ApiLogOptions options,
        ILoggerFactory? loggerFactory = null)
    {
        var updated = ApiLogOptionsFactory.CreateNormalized(options);
        var formatter = ResolveDefaultFormatter<TPayload>();
        return new ApiLogRuntime<TPayload>(updated, formatter, loggerFactory);
    }

    public static IApiLogRuntime<TPayload> Create<TPayload>(
        IConfiguration configuration,
        ILoggerFactory? loggerFactory = null)
    {
        var options = ApiLogOptionsFactory.CreateNormalized(configuration);
        var formatter = ResolveDefaultFormatter<TPayload>();
        return new ApiLogRuntime<TPayload>(options, formatter, loggerFactory);
    }

    // Ready-made factory overloads for ApiLogMessagePayload

    public static IApiLogRuntime<ApiLogMessagePayload> Create(
        ILoggerFactory? loggerFactory = null)
    {
        var options = ApiLogOptionsFactory.CreateDefault();
        return new ApiLogRuntime<ApiLogMessagePayload>(options, new DefaultApiLogMessageFormatter(), loggerFactory);
    }

    public static IApiLogRuntime<ApiLogMessagePayload> Create(
        IApiLogFormatter<ApiLogMessagePayload> formatter,
        ILoggerFactory? loggerFactory = null)
    {
        var options = ApiLogOptionsFactory.CreateDefault();
        return new ApiLogRuntime<ApiLogMessagePayload>(options, formatter, loggerFactory);
    }

    public static IApiLogRuntime<ApiLogMessagePayload> Create(
        Action<ApiLogOptions> configure,
        ILoggerFactory? loggerFactory = null)
    {
        var options = ApiLogOptionsFactory.CreateNormalized(configure);
        return new ApiLogRuntime<ApiLogMessagePayload>(options, new DefaultApiLogMessageFormatter(), loggerFactory);
    }

    public static IApiLogRuntime<ApiLogMessagePayload> Create(
        Action<ApiLogOptions> configure,
        IApiLogFormatter<ApiLogMessagePayload> formatter,
        ILoggerFactory? loggerFactory = null)
    {
        var options = ApiLogOptionsFactory.CreateNormalized(configure);
        return new ApiLogRuntime<ApiLogMessagePayload>(options, formatter, loggerFactory);
    }

    public static IApiLogRuntime<ApiLogMessagePayload> Create(
        ApiLogOptions options,
        ILoggerFactory? loggerFactory = null)
    {
        var updated = ApiLogOptionsFactory.CreateNormalized(options);
        return new ApiLogRuntime<ApiLogMessagePayload>(updated, new DefaultApiLogMessageFormatter(), loggerFactory);
    }

    public static IApiLogRuntime<ApiLogMessagePayload> Create(
        ApiLogOptions options,
        IApiLogFormatter<ApiLogMessagePayload> formatter,
        ILoggerFactory? loggerFactory = null)
    {
        var updated = ApiLogOptionsFactory.CreateNormalized(options);
        return new ApiLogRuntime<ApiLogMessagePayload>(updated, formatter, loggerFactory);
    }

    // Configuration-based factory overloads for ApiLogMessagePayload

    public static IApiLogRuntime<ApiLogMessagePayload> Create(
        IConfiguration configuration,
        ILoggerFactory? loggerFactory = null)
    {
        var options = ApiLogOptionsFactory.CreateNormalized(configuration);
        return new ApiLogRuntime<ApiLogMessagePayload>(options, new DefaultApiLogMessageFormatter(), loggerFactory);
    }

    public static IApiLogRuntime<ApiLogMessagePayload> Create(
        IConfiguration configuration,
        IApiLogFormatter<ApiLogMessagePayload> formatter,
        ILoggerFactory? loggerFactory = null)
    {
        var options = ApiLogOptionsFactory.CreateNormalized(configuration);
        return new ApiLogRuntime<ApiLogMessagePayload>(options, formatter, loggerFactory);
    }
}