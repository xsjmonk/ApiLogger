using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiLogger;

public static class ApiLoggerFactory
{
    private static string ResolveCategory(string? categoryName)
        => string.IsNullOrWhiteSpace(categoryName) ? "ApiLogger" : categoryName;

    public static ApiLoggerHandle CreateLogger(string? categoryName = null)
    {
        var category = ResolveCategory(categoryName);
        var runtime = ApiLogRuntimeFactory.Create();
        return CreateHandle(category, runtime);
    }

    public static ApiLoggerHandle CreateLogger(IConfiguration configuration)
    {
        var runtime = ApiLogRuntimeFactory.Create(configuration);
        return CreateHandle("ApiLogger", runtime);
    }

    public static ApiLoggerHandle CreateLogger(Action<ApiLogOptions> configure)
    {
        var runtime = ApiLogRuntimeFactory.Create(configure);
        return CreateHandle("ApiLogger", runtime);
    }

    public static ApiLoggerHandle CreateLogger(ApiLogOptions options)
    {
        var runtime = ApiLogRuntimeFactory.Create(options);
        return CreateHandle("ApiLogger", runtime);
    }

    public static ApiLoggerHandle CreateLogger(string? categoryName, IConfiguration configuration)
    {
        var category = ResolveCategory(categoryName);
        var runtime = ApiLogRuntimeFactory.Create(configuration);
        return CreateHandle(category, runtime);
    }

    public static ApiLoggerHandle CreateLogger(string? categoryName, Action<ApiLogOptions> configure)
    {
        var category = ResolveCategory(categoryName);
        var runtime = ApiLogRuntimeFactory.Create(configure);
        return CreateHandle(category, runtime);
    }

    public static ApiLoggerHandle CreateLogger(string? categoryName, ApiLogOptions options)
    {
        var category = ResolveCategory(categoryName);
        var runtime = ApiLogRuntimeFactory.Create(options);
        return CreateHandle(category, runtime);
    }

    public static ApiLoggerHandle CreateLogger<TCategory>()
        => CreateLogger(typeof(TCategory).FullName ?? typeof(TCategory).Name);

    public static ApiLoggerHandle CreateLogger<TCategory>(IConfiguration configuration)
        => CreateLogger(typeof(TCategory).FullName ?? typeof(TCategory).Name, configuration);

    public static ApiLoggerHandle CreateLogger<TCategory>(Action<ApiLogOptions> configure)
        => CreateLogger(typeof(TCategory).FullName ?? typeof(TCategory).Name, configure);

    public static ApiLoggerHandle CreateLogger<TCategory>(ApiLogOptions options)
        => CreateLogger(typeof(TCategory).FullName ?? typeof(TCategory).Name, options);

    private static ApiLoggerHandle CreateHandle(string categoryName, IApiLogRuntime<ApiLogMessagePayload> runtime)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new ApiLoggerProvider<ApiLogMessagePayload>(runtime, new ApiLogMessagePayloadFactory()));
        });

        var logger = loggerFactory.CreateLogger(categoryName);
        return new ApiLoggerHandle(logger, loggerFactory, runtime);
    }
}
