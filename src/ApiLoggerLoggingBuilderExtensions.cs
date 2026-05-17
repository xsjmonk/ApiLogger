using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace ApiLogger;

public static class ApiLoggerLoggingBuilderExtensions
{
    public static ILoggingBuilder AddApiLogger(this ILoggingBuilder builder)
    {
        AddRuntime(builder.Services, null, null);
        return builder;
    }

    public static ILoggingBuilder AddApiLogger(this ILoggingBuilder builder, Action<ApiLogOptions> configure)
    {
        AddRuntime(builder.Services, configure, null);
        return builder;
    }

    public static ILoggingBuilder AddApiLogger(this ILoggingBuilder builder, IConfiguration configuration)
    {
        AddRuntime(builder.Services, null, configuration);
        return builder;
    }

    public static ILoggingBuilder AddApiLogger(this ILoggingBuilder builder, ApiLogOptions options)
    {
        builder.Services.AddSingleton<IApiLogFormatter<ApiLogMessagePayload>>(new DefaultApiLogMessageFormatter());
        builder.Services.AddSingleton(options);
        AddCommon(builder.Services);
        TryAddProvider(builder.Services);
        return builder;
    }

    private static void AddRuntime(IServiceCollection services, Action<ApiLogOptions>? configure, IConfiguration? configuration)
    {
        ApiLogOptions options;
        if (configuration != null)
        {
            options = ApiLogOptionsFactory.CreateNormalized(configuration);
        }
        else if (configure != null)
        {
            options = ApiLogOptionsFactory.CreateNormalized(configure);
        }
        else
        {
            options = ApiLogOptionsFactory.CreateDefault();
        }

        services.AddSingleton<IApiLogFormatter<ApiLogMessagePayload>>(new DefaultApiLogMessageFormatter());
        services.AddSingleton(options);
        AddCommon(services);
        TryAddProvider(services);
    }

    private static void AddCommon(IServiceCollection services)
    {
        services.AddSingleton<ApiLogQueue<ApiLogMessagePayload>>();
        services.AddSingleton<IApiLogQueue<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogQueue<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogWriter<ApiLogMessagePayload>, ApiLogWriter<ApiLogMessagePayload>>();
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>, ApiPayloadLogger<ApiLogMessagePayload>>();
        services.AddSingleton<ApiLogRuntime<ApiLogMessagePayload>>(sp =>
            new ApiLogRuntime<ApiLogMessagePayload>(
                sp.GetRequiredService<ApiLogOptions>(),
                sp.GetRequiredService<IApiLogFormatter<ApiLogMessagePayload>>(),
                null));
        services.AddSingleton<IApiLogRuntime<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogRuntime<ApiLogMessagePayload>>());
        services.AddSingleton<IApiPayloadLogger<ApiLogMessagePayload>>(sp => sp.GetRequiredService<ApiLogRuntime<ApiLogMessagePayload>>());
        services.AddSingleton<IApiLogger, ApiLogger>(sp =>
            new ApiLogger(sp.GetRequiredService<IApiPayloadLogger<ApiLogMessagePayload>>()));
    }

    private static void TryAddProvider(IServiceCollection services)
    {
        services.TryAddSingleton<IApiLogPayloadFactory<ApiLogMessagePayload>>(new ApiLogMessagePayloadFactory());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ApiLoggerProvider<ApiLogMessagePayload>>(sp =>
            new ApiLoggerProvider<ApiLogMessagePayload>(
                sp.GetRequiredService<IApiPayloadLogger<ApiLogMessagePayload>>(),
                sp.GetRequiredService<IApiLogPayloadFactory<ApiLogMessagePayload>>())));
    }
}
