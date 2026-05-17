using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiLogger;

public static class ApiLoggerServiceCollectionExtensions
{
    private static ApiLogOptions CreateNormalizedOptions(Action<ApiLogOptions> configure)
    {
        const long defaultBytes = 10 * 1024 * 1024;

        var options = new ApiLogOptions();
        configure(options);
        options.RotateSizeBytes = ApiLogOptions.ParseRotateSizeBytesOrDefault(options.RotateSize, defaultBytes);
        ApiLogOptionsFactory.NormalizeFileName(options);
        return options;
    }

    private static ApiLogOptions CreateNormalizedOptions(IConfiguration configuration)
    {
        return ApiLogOptionsFactory.CreateNormalized(configuration);
    }

    private static ApiLogOptions CreateDefaultOptions()
    {
        return ApiLogOptionsFactory.CreateDefault();
    }

    private static IApiLogFormatter<TPayload> ResolveDefaultFormatter<TPayload>()
    {
        if (typeof(TPayload) == typeof(ApiLogMessagePayload))
        {
            return (IApiLogFormatter<TPayload>)(object)new DefaultApiLogMessageFormatter();
        }
        return new DefaultApiLogFormatter<TPayload>();
    }

    // Generic typed payload with IHostedService

    public static IServiceCollection AddApiLogger<TPayload>(
        this IServiceCollection services,
        Action<ApiLogOptions> configure)
    {
        services.AddSingleton<IApiLogFormatter<TPayload>>(ResolveDefaultFormatter<TPayload>());
        services.AddSingleton<ApiLogOptions>(sp => CreateNormalizedOptions(configure));

        services.AddSingleton<ApiLogQueue<TPayload>>();
        services.AddSingleton<IApiLogQueue<TPayload>>(sp => sp.GetRequiredService<ApiLogQueue<TPayload>>());
        services.AddSingleton<IApiLogWriter<TPayload>, ApiLogWriter<TPayload>>();
        services.AddSingleton<IApiPayloadLogger<TPayload>, ApiPayloadLogger<TPayload>>();

        services.AddHostedService(sp =>
            new ApiLogHostedService<TPayload>(
                sp.GetRequiredService<ApiLogQueue<TPayload>>(),
                sp.GetRequiredService<IApiLogWriter<TPayload>>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApiLogHostedService<TPayload>>>()));

        return services;
    }

    public static IServiceCollection AddApiLogger<TPayload>(
        this IServiceCollection services,
        Action<ApiLogOptions> configure,
        IApiLogFormatter<TPayload> formatter)
    {
        services.AddSingleton<IApiLogFormatter<TPayload>>(formatter);
        services.AddSingleton<ApiLogOptions>(sp => CreateNormalizedOptions(configure));
        services.AddSingleton<ApiLogQueue<TPayload>>();
        services.AddSingleton<IApiLogQueue<TPayload>>(sp => sp.GetRequiredService<ApiLogQueue<TPayload>>());
        services.AddSingleton<IApiLogWriter<TPayload>, ApiLogWriter<TPayload>>();
        services.AddSingleton<IApiPayloadLogger<TPayload>, ApiPayloadLogger<TPayload>>();
        services.AddHostedService(sp =>
            new ApiLogHostedService<TPayload>(
                sp.GetRequiredService<ApiLogQueue<TPayload>>(),
                sp.GetRequiredService<IApiLogWriter<TPayload>>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApiLogHostedService<TPayload>>>()));
        return services;
    }

    // IConfiguration overloads for generic payload

    public static IServiceCollection AddApiLogger<TPayload>(
        this IServiceCollection services,
        IConfiguration configuration,
        IApiLogFormatter<TPayload> formatter)
    {
        services.AddSingleton<IApiLogFormatter<TPayload>>(formatter);
        services.AddSingleton<ApiLogOptions>(sp => CreateNormalizedOptions(configuration));
        services.AddSingleton<ApiLogQueue<TPayload>>();
        services.AddSingleton<IApiLogQueue<TPayload>>(sp => sp.GetRequiredService<ApiLogQueue<TPayload>>());
        services.AddSingleton<IApiLogWriter<TPayload>, ApiLogWriter<TPayload>>();
        services.AddSingleton<IApiPayloadLogger<TPayload>, ApiPayloadLogger<TPayload>>();

        services.AddHostedService(sp =>
            new ApiLogHostedService<TPayload>(
                sp.GetRequiredService<ApiLogQueue<TPayload>>(),
                sp.GetRequiredService<IApiLogWriter<TPayload>>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApiLogHostedService<TPayload>>>()));

        return services;
    }

    public static IServiceCollection AddApiLogger<TPayload>(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddApiLogger<TPayload>(configuration, ResolveDefaultFormatter<TPayload>());
    }

    // Ready-made IApiLogger overloads with IHostedService

    public static IServiceCollection AddApiLogger(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IApiLogFormatter<ApiLogMessagePayload>>(new DefaultApiLogMessageFormatter());
        services.AddSingleton<ApiLogOptions>(sp => CreateNormalizedOptions(configuration));
        services.AddSingleton<ApiLogQueue<ApiLogMessagePayload>>();
        services.AddSingleton<IApiLogQueue<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogQueue<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogWriter<ApiLogMessagePayload>, ApiLogWriter<ApiLogMessagePayload>>();
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>, ApiPayloadLogger<ApiLogMessagePayload>>();
        services.AddSingleton<IApiLogger, ApiLogger>(sp =>
            new ApiLogger(sp.GetRequiredService<IApiPayloadLogger<ApiLogMessagePayload>>()));

        services.AddHostedService(sp =>
            new ApiLogHostedService<ApiLogMessagePayload>(
                sp.GetRequiredService<ApiLogQueue<ApiLogMessagePayload>>(),
                sp.GetRequiredService<IApiLogWriter<ApiLogMessagePayload>>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApiLogHostedService<ApiLogMessagePayload>>>()));

        return services;
    }

    public static IServiceCollection AddApiLogger(
        this IServiceCollection services,
        IConfiguration configuration,
        IApiLogFormatter<ApiLogMessagePayload> formatter)
    {
        services.AddSingleton<IApiLogFormatter<ApiLogMessagePayload>>(formatter);
        services.AddSingleton<ApiLogOptions>(sp => CreateNormalizedOptions(configuration));
        services.AddSingleton<ApiLogQueue<ApiLogMessagePayload>>();
        services.AddSingleton<IApiLogQueue<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogQueue<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogWriter<ApiLogMessagePayload>, ApiLogWriter<ApiLogMessagePayload>>();
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>, ApiPayloadLogger<ApiLogMessagePayload>>();
        services.AddSingleton<IApiLogger, ApiLogger>(sp =>
            new ApiLogger(sp.GetRequiredService<IApiPayloadLogger<ApiLogMessagePayload>>()));

        services.AddHostedService(sp =>
            new ApiLogHostedService<ApiLogMessagePayload>(
                sp.GetRequiredService<ApiLogQueue<ApiLogMessagePayload>>(),
                sp.GetRequiredService<IApiLogWriter<ApiLogMessagePayload>>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApiLogHostedService<ApiLogMessagePayload>>>()));

        return services;
    }

    public static IServiceCollection AddApiLogger(
        this IServiceCollection services)
    {
        var options = CreateDefaultOptions();
        services.AddSingleton<ApiLogOptions>(options);
        services.AddSingleton<IApiLogFormatter<ApiLogMessagePayload>>(new DefaultApiLogMessageFormatter());
        services.AddSingleton<ApiLogQueue<ApiLogMessagePayload>>();
        services.AddSingleton<IApiLogQueue<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogQueue<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogWriter<ApiLogMessagePayload>, ApiLogWriter<ApiLogMessagePayload>>();
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>, ApiPayloadLogger<ApiLogMessagePayload>>();
        services.AddSingleton<IApiLogger, ApiLogger>(sp =>
            new ApiLogger(sp.GetRequiredService<IApiPayloadLogger<ApiLogMessagePayload>>()));

        services.AddHostedService(sp =>
            new ApiLogHostedService<ApiLogMessagePayload>(
                sp.GetRequiredService<ApiLogQueue<ApiLogMessagePayload>>(),
                sp.GetRequiredService<IApiLogWriter<ApiLogMessagePayload>>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApiLogHostedService<ApiLogMessagePayload>>>()));

        return services;
    }

    public static IServiceCollection AddApiLogger(
        this IServiceCollection services,
        IApiLogFormatter<ApiLogMessagePayload> formatter)
    {
        var options = CreateDefaultOptions();
        services.AddSingleton<ApiLogOptions>(options);
        services.AddSingleton<IApiLogFormatter<ApiLogMessagePayload>>(formatter);
        services.AddSingleton<ApiLogQueue<ApiLogMessagePayload>>();
        services.AddSingleton<IApiLogQueue<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogQueue<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogWriter<ApiLogMessagePayload>, ApiLogWriter<ApiLogMessagePayload>>();
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>, ApiPayloadLogger<ApiLogMessagePayload>>();
        services.AddSingleton<IApiLogger, ApiLogger>(sp =>
            new ApiLogger(sp.GetRequiredService<IApiPayloadLogger<ApiLogMessagePayload>>()));

        services.AddHostedService(sp =>
            new ApiLogHostedService<ApiLogMessagePayload>(
                sp.GetRequiredService<ApiLogQueue<ApiLogMessagePayload>>(),
                sp.GetRequiredService<IApiLogWriter<ApiLogMessagePayload>>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApiLogHostedService<ApiLogMessagePayload>>>()));

        return services;
    }

    // Provider-based logging

    public static IServiceCollection AddApiLoggerProvider<TPayload>(
        this IServiceCollection services,
        Action<ApiLogOptions> configure,
        IApiLogFormatter<TPayload> formatter,
        IApiLogPayloadFactory<TPayload> payloadFactory)
    {
        services.AddSingleton<IApiLogPayloadFactory<TPayload>>(payloadFactory);
        services.AddApiLogger<TPayload>(configure, formatter);

        services.AddSingleton<ILoggerProvider, ApiLoggerProvider<TPayload>>(sp =>
            new ApiLoggerProvider<TPayload>(
                sp.GetRequiredService<IApiPayloadLogger<TPayload>>(),
                sp.GetRequiredService<IApiLogPayloadFactory<TPayload>>()));

        return services;
    }

    // Runtime-only generic typed overloads (no IHostedService)

    public static IServiceCollection AddApiLoggerRuntime<TPayload>(
        this IServiceCollection services,
        Action<ApiLogOptions> configure)
    {
        services.AddSingleton<IApiLogFormatter<TPayload>>(ResolveDefaultFormatter<TPayload>());
        services.AddSingleton<ApiLogOptions>(sp => CreateNormalizedOptions(configure));
        services.AddSingleton<ApiLogQueue<TPayload>>();
        services.AddSingleton<IApiLogQueue<TPayload>>(sp => sp.GetRequiredService<ApiLogQueue<TPayload>>());
        services.AddSingleton<IApiLogWriter<TPayload>, ApiLogWriter<TPayload>>();
        services.AddSingleton<IApiPayloadLogger<TPayload>, ApiPayloadLogger<TPayload>>();
        services.AddSingleton<ApiLogRuntime<TPayload>>(sp =>
            new ApiLogRuntime<TPayload>(
                sp.GetRequiredService<ApiLogOptions>(),
                sp.GetRequiredService<IApiLogFormatter<TPayload>>(),
                sp.GetService<ILoggerFactory>()));
        services.AddSingleton<IApiLogRuntime<TPayload>>(sp => sp.GetRequiredService<ApiLogRuntime<TPayload>>());
        services.AddSingleton<IApiPayloadLogger<TPayload>>(sp => sp.GetRequiredService<ApiLogRuntime<TPayload>>());

        return services;
    }

    public static IServiceCollection AddApiLoggerRuntime<TPayload>(
        this IServiceCollection services,
        Action<ApiLogOptions> configure,
        IApiLogFormatter<TPayload> formatter)
    {
        services.AddSingleton<IApiLogFormatter<TPayload>>(formatter);
        services.AddSingleton<ApiLogOptions>(sp => CreateNormalizedOptions(configure));
        services.AddSingleton<ApiLogQueue<TPayload>>();
        services.AddSingleton<IApiLogQueue<TPayload>>(sp => sp.GetRequiredService<ApiLogQueue<TPayload>>());
        services.AddSingleton<IApiLogWriter<TPayload>, ApiLogWriter<TPayload>>();
        services.AddSingleton<IApiPayloadLogger<TPayload>, ApiPayloadLogger<TPayload>>();
        services.AddSingleton<ApiLogRuntime<TPayload>>(sp =>
            new ApiLogRuntime<TPayload>(
                sp.GetRequiredService<ApiLogOptions>(),
                sp.GetRequiredService<IApiLogFormatter<TPayload>>(),
                sp.GetService<ILoggerFactory>()));
        services.AddSingleton<IApiLogRuntime<TPayload>>(sp => sp.GetRequiredService<ApiLogRuntime<TPayload>>());
        services.AddSingleton<IApiPayloadLogger<TPayload>>(sp => sp.GetRequiredService<ApiLogRuntime<TPayload>>());

        return services;
    }

    public static IServiceCollection AddApiLoggerRuntime<TPayload>(
        this IServiceCollection services,
        IConfiguration configuration,
        IApiLogFormatter<TPayload> formatter)
    {
        services.AddSingleton<IApiLogFormatter<TPayload>>(formatter);
        services.AddSingleton<ApiLogOptions>(sp => CreateNormalizedOptions(configuration));
        services.AddSingleton<ApiLogQueue<TPayload>>();
        services.AddSingleton<IApiLogQueue<TPayload>>(sp => sp.GetRequiredService<ApiLogQueue<TPayload>>());
        services.AddSingleton<IApiLogWriter<TPayload>, ApiLogWriter<TPayload>>();
        services.AddSingleton<IApiPayloadLogger<TPayload>, ApiPayloadLogger<TPayload>>();
        services.AddSingleton<ApiLogRuntime<TPayload>>(sp =>
            new ApiLogRuntime<TPayload>(
                sp.GetRequiredService<ApiLogOptions>(),
                sp.GetRequiredService<IApiLogFormatter<TPayload>>(),
                sp.GetService<ILoggerFactory>()));
        services.AddSingleton<IApiLogRuntime<TPayload>>(sp => sp.GetRequiredService<ApiLogRuntime<TPayload>>());
        services.AddSingleton<IApiPayloadLogger<TPayload>>(sp => sp.GetRequiredService<ApiLogRuntime<TPayload>>());

        return services;
    }

    public static IServiceCollection AddApiLoggerRuntime<TPayload>(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddApiLoggerRuntime<TPayload>(configuration, ResolveDefaultFormatter<TPayload>());
    }

    // Ready-made runtime-only overloads (no IHostedService)

    public static IServiceCollection AddApiLoggerRuntime(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IApiLogFormatter<ApiLogMessagePayload>>(new DefaultApiLogMessageFormatter());
        services.AddSingleton<ApiLogOptions>(sp => CreateNormalizedOptions(configuration));
        services.AddSingleton<ApiLogQueue<ApiLogMessagePayload>>();
        services.AddSingleton<IApiLogQueue<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogQueue<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogWriter<ApiLogMessagePayload>, ApiLogWriter<ApiLogMessagePayload>>();
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>, ApiPayloadLogger<ApiLogMessagePayload>>();
        services.AddSingleton<ApiLogRuntime<ApiLogMessagePayload>>(sp =>
            new ApiLogRuntime<ApiLogMessagePayload>(
                sp.GetRequiredService<ApiLogOptions>(),
                sp.GetRequiredService<IApiLogFormatter<ApiLogMessagePayload>>(),
                sp.GetService<ILoggerFactory>()));
        services.AddSingleton<IApiLogRuntime<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogRuntime<ApiLogMessagePayload>>());
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogRuntime<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogger, ApiLogger>(sp =>
            new ApiLogger(sp.GetRequiredService<IApiPayloadLogger<ApiLogMessagePayload>>()));

        return services;
    }

    public static IServiceCollection AddApiLoggerRuntime(
        this IServiceCollection services,
        IConfiguration configuration,
        IApiLogFormatter<ApiLogMessagePayload> formatter)
    {
        services.AddSingleton<IApiLogFormatter<ApiLogMessagePayload>>(formatter);
        services.AddSingleton<ApiLogOptions>(sp => CreateNormalizedOptions(configuration));
        services.AddSingleton<ApiLogQueue<ApiLogMessagePayload>>();
        services.AddSingleton<IApiLogQueue<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogQueue<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogWriter<ApiLogMessagePayload>, ApiLogWriter<ApiLogMessagePayload>>();
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>, ApiPayloadLogger<ApiLogMessagePayload>>();
        services.AddSingleton<ApiLogRuntime<ApiLogMessagePayload>>(sp =>
            new ApiLogRuntime<ApiLogMessagePayload>(
                sp.GetRequiredService<ApiLogOptions>(),
                sp.GetRequiredService<IApiLogFormatter<ApiLogMessagePayload>>(),
                sp.GetService<ILoggerFactory>()));
        services.AddSingleton<IApiLogRuntime<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogRuntime<ApiLogMessagePayload>>());
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogRuntime<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogger, ApiLogger>(sp =>
            new ApiLogger(sp.GetRequiredService<IApiPayloadLogger<ApiLogMessagePayload>>()));

        return services;
    }

    public static IServiceCollection AddApiLoggerRuntime(
        this IServiceCollection services)
    {
        var options = CreateDefaultOptions();
        services.AddSingleton<ApiLogOptions>(options);
        services.AddSingleton<IApiLogFormatter<ApiLogMessagePayload>>(new DefaultApiLogMessageFormatter());
        services.AddSingleton<ApiLogQueue<ApiLogMessagePayload>>();
        services.AddSingleton<IApiLogQueue<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogQueue<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogWriter<ApiLogMessagePayload>, ApiLogWriter<ApiLogMessagePayload>>();
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>, ApiPayloadLogger<ApiLogMessagePayload>>();
        services.AddSingleton<ApiLogRuntime<ApiLogMessagePayload>>(sp =>
            new ApiLogRuntime<ApiLogMessagePayload>(
                sp.GetRequiredService<ApiLogOptions>(),
                sp.GetRequiredService<IApiLogFormatter<ApiLogMessagePayload>>(),
                sp.GetService<ILoggerFactory>()));
        services.AddSingleton<IApiLogRuntime<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogRuntime<ApiLogMessagePayload>>());
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogRuntime<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogger, ApiLogger>(sp =>
            new ApiLogger(sp.GetRequiredService<IApiPayloadLogger<ApiLogMessagePayload>>()));

        return services;
    }

    public static IServiceCollection AddApiLoggerRuntime(
        this IServiceCollection services,
        IApiLogFormatter<ApiLogMessagePayload> formatter)
    {
        var options = CreateDefaultOptions();
        services.AddSingleton<ApiLogOptions>(options);
        services.AddSingleton<IApiLogFormatter<ApiLogMessagePayload>>(formatter);
        services.AddSingleton<ApiLogQueue<ApiLogMessagePayload>>();
        services.AddSingleton<IApiLogQueue<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogQueue<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogWriter<ApiLogMessagePayload>, ApiLogWriter<ApiLogMessagePayload>>();
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>, ApiPayloadLogger<ApiLogMessagePayload>>();
        services.AddSingleton<ApiLogRuntime<ApiLogMessagePayload>>(sp =>
            new ApiLogRuntime<ApiLogMessagePayload>(
                sp.GetRequiredService<ApiLogOptions>(),
                sp.GetRequiredService<IApiLogFormatter<ApiLogMessagePayload>>(),
                sp.GetService<ILoggerFactory>()));
        services.AddSingleton<IApiLogRuntime<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogRuntime<ApiLogMessagePayload>>());
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogRuntime<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogger, ApiLogger>(sp =>
            new ApiLogger(sp.GetRequiredService<IApiPayloadLogger<ApiLogMessagePayload>>()));

        return services;
    }

    public static IServiceCollection AddApiLoggerRuntime(
        this IServiceCollection services,
        Action<ApiLogOptions> configure)
    {
        services.AddSingleton<ApiLogOptions>(sp => CreateNormalizedOptions(configure));
        services.AddSingleton<IApiLogFormatter<ApiLogMessagePayload>>(new DefaultApiLogMessageFormatter());
        services.AddSingleton<ApiLogQueue<ApiLogMessagePayload>>();
        services.AddSingleton<IApiLogQueue<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogQueue<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogWriter<ApiLogMessagePayload>, ApiLogWriter<ApiLogMessagePayload>>();
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>, ApiPayloadLogger<ApiLogMessagePayload>>();
        services.AddSingleton<ApiLogRuntime<ApiLogMessagePayload>>(sp =>
            new ApiLogRuntime<ApiLogMessagePayload>(
                sp.GetRequiredService<ApiLogOptions>(),
                sp.GetRequiredService<IApiLogFormatter<ApiLogMessagePayload>>(),
                sp.GetService<ILoggerFactory>()));
        services.AddSingleton<IApiLogRuntime<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogRuntime<ApiLogMessagePayload>>());
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogRuntime<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogger, ApiLogger>(sp =>
            new ApiLogger(sp.GetRequiredService<IApiPayloadLogger<ApiLogMessagePayload>>()));

        return services;
    }

    public static IServiceCollection AddApiLoggerRuntime(
        this IServiceCollection services,
        Action<ApiLogOptions> configure,
        IApiLogFormatter<ApiLogMessagePayload> formatter)
    {
        services.AddSingleton<IApiLogFormatter<ApiLogMessagePayload>>(formatter);
        services.AddSingleton<ApiLogOptions>(sp => CreateNormalizedOptions(configure));
        services.AddSingleton<ApiLogQueue<ApiLogMessagePayload>>();
        services.AddSingleton<IApiLogQueue<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogQueue<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogWriter<ApiLogMessagePayload>, ApiLogWriter<ApiLogMessagePayload>>();
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>, ApiPayloadLogger<ApiLogMessagePayload>>();
        services.AddSingleton<ApiLogRuntime<ApiLogMessagePayload>>(sp =>
            new ApiLogRuntime<ApiLogMessagePayload>(
                sp.GetRequiredService<ApiLogOptions>(),
                sp.GetRequiredService<IApiLogFormatter<ApiLogMessagePayload>>(),
                sp.GetService<ILoggerFactory>()));
        services.AddSingleton<IApiLogRuntime<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogRuntime<ApiLogMessagePayload>>());
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogRuntime<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogger, ApiLogger>(sp =>
            new ApiLogger(sp.GetRequiredService<IApiPayloadLogger<ApiLogMessagePayload>>()));

        return services;
    }
}