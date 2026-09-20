namespace GoalExtraction.Infrastructure;

using System.Net;
using System.Net.Http.Headers;
using GoalExtraction.Application.AI;
using GoalExtraction.Application.AI.Tools;
using GoalExtraction.Domain.Interfaces;
using GoalExtraction.Infrastructure.AI;
using GoalExtraction.Infrastructure.AI.Options;
using GoalExtraction.Infrastructure.Persistence;
using GoalExtraction.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        AddInfrastructurePersistence(services, configuration);
        AddInfrastructureAi(services, configuration);
        return services;
    }

    public static IServiceCollection AddInfrastructurePersistence(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=goals.db;Cache=Shared;";

        // Primary read-write DbContext for migrations, seeding, and batch mutations
        services.AddDbContext<GoalExtractionDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        // Physical DB Connection Factory with separate Read-Only and Read-Write pools
        services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(connectionString));
        services.AddSingleton<DbConnectionFactory>(sp => (DbConnectionFactory)sp.GetRequiredService<IDbConnectionFactory>());

        // Dedicated physically isolated Read-Only DbContext (AC 4.1 & AC 4.3)
        // Enforces SQLite Mode=ReadOnly and executes PRAGMA query_only = ON upon connection open
        services.AddDbContext<ReadOnlyGoalExtractionDbContext>((sp, options) =>
        {
            var connectionFactory = sp.GetRequiredService<IDbConnectionFactory>();
            options.UseSqlite(connectionFactory.ReadOnlyConnectionString)
                   .AddInterceptors(new ReadOnlyConnectionInterceptor())
                   .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        // Repositories
        services.AddScoped<IReadOnlyGoalQueryRepository>(sp =>
            new ReadOnlyGoalQueryRepository(sp.GetRequiredService<ReadOnlyGoalExtractionDbContext>()));
        services.AddScoped<ReadOnlyGoalQueryRepository>(sp => 
            (ReadOnlyGoalQueryRepository)sp.GetRequiredService<IReadOnlyGoalQueryRepository>());
        services.AddScoped<IGoalBatchWriteRepository, GoalBatchWriteRepository>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }

    public static IServiceCollection AddInfrastructureAi(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var options = new GeminiAiOptions();
        configuration.GetSection(GeminiAiOptions.SectionName).Bind(options);

        // Environment variable & configuration overrides:
        // Priority inspection order: TITAN_* -> AI_LLM_* -> GEMINI_* (AC 3.1 & Description.md)
        string? ResolveSetting(params string[] keys)
        {
            foreach (var key in keys)
            {
                var envVal = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrWhiteSpace(envVal)) return envVal;
                var cfgVal = configuration[key];
                if (!string.IsNullOrWhiteSpace(cfgVal)) return cfgVal;
            }
            return null;
        }

        var resolvedApiKey = ResolveSetting("TITAN_API_KEY", "TITAN_LLM_API_KEY", "AI_LLM_API_KEY", "GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(resolvedApiKey))
        {
            options.ApiKey = resolvedApiKey;
        }

        var resolvedBaseUrl = ResolveSetting("TITAN_LLM_BASE_URL", "TITAN_BASE_URL", "AI_LLM_BASE_URL", "GEMINI_BASE_URL");
        if (!string.IsNullOrWhiteSpace(resolvedBaseUrl))
        {
            options.BaseUrl = resolvedBaseUrl;
        }

        var resolvedModel = ResolveSetting("TITAN_LLM_MODEL", "TITAN_MODEL", "AI_LLM_MODEL", "GEMINI_MODEL");
        if (!string.IsNullOrWhiteSpace(resolvedModel))
        {
            options.Model = resolvedModel;
        }

        services.Configure<GeminiAiOptions>(opt =>
        {
            opt.BaseUrl = options.BaseUrl;
            opt.ApiKey = options.ApiKey;
            opt.Model = options.Model;
            opt.TimeoutSeconds = options.TimeoutSeconds;
        });

        services.AddHttpClient<IGeminiOpenAiClient, GeminiOpenAiClient>((sp, client) =>
        {
            var currentOptions = sp.GetRequiredService<IOptions<GeminiAiOptions>>().Value;
            var baseUrl = currentOptions.BaseUrl.EndsWith('/') ? currentOptions.BaseUrl : currentOptions.BaseUrl + "/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(currentOptions.TimeoutSeconds);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", currentOptions.ApiKey);
        })
        .AddTransientHttpErrorPolicy(policy =>
            policy.OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
                  .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Min(12, Math.Pow(2, retryAttempt)))))
        .AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

        services.AddScoped<IReadOnlyGoalQueryTool, ReadOnlyGoalQueryTool>();
        services.AddScoped<ReadOnlyGoalQueryTool>(sp => (ReadOnlyGoalQueryTool)sp.GetRequiredService<IReadOnlyGoalQueryTool>());
        services.AddSingleton<JsonSchemaRepairService>();
        services.AddScoped<IGoalExtractionAgent, GoalExtractionAgent>();
        services.AddScoped<GoalExtractionAgent>(sp => (GoalExtractionAgent)sp.GetRequiredService<IGoalExtractionAgent>());

        return services;
    }
}
