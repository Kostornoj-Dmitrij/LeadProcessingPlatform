using System.Text.Json;
using System.Text.Json.Serialization;
using AvroSchemas.Naming;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedHosting.Constants;
using SharedHosting.Extensions;
using SharedHosting.Middleware;
using SharedHosting.Options;

namespace SharedHosting;

/// <summary>
/// Расширения для настройки хоста микросервиса
/// </summary>
public static class HostBuilderExtensions
{
    public static IServiceCollection AddSharedHosting(
        this IServiceCollection services,
        IConfiguration configuration,
        HostingOptions hostingOptions,
        string[]? additionalTelemetrySources = null)
    {
        services.Configure<NamingOptions>(configuration.GetSection(NamingOptions.SectionName));
        services.AddSingleton<INamingConvention, NamingConvention>();

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddSharedOpenTelemetry(configuration, hostingOptions.ServiceName, additionalTelemetrySources);

        if (hostingOptions.EnableHealthChecks)
        {
            services.AddSharedHealthChecks(configuration, hostingOptions.ServiceName);
        }

        if (hostingOptions.EnableSwagger)
        {
            services.AddOpenApiDocument(config =>
            {
                config.DocumentName = "v1";
                config.Title = $"{hostingOptions.ServiceName} API";
                config.Version = "v1";
                config.Description = $"API for {hostingOptions.ServiceName} microservice";
            });
        }

        return services;
    }

    public static WebApplication UseSharedHosting(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionHandler>();
        app.UseMiddleware<RequestLoggingMiddleware>();

        app.UseRouting();
        app.UseAuthorization();

        app.MapControllers();

        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();
            app.UseSwaggerUi(config =>
            {
                config.DocumentTitle = $"{app.Environment.ApplicationName} API";
                config.Path = ConfigurationKeys.SwaggerPath;
                config.DocumentPath = ConfigurationKeys.SwaggerJsonPath;
            });
        }

        app.MapHealthChecks(ConfigurationKeys.HealthPath);

        return app;
    }

    public static async Task ApplyMigrationsAsync<TContext>(
        this WebApplication app,
        Func<IServiceProvider, TContext> contextFactory)
        where TContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<object>>();

        try
        {
            var context = contextFactory(services);
            logger.LogInformation("Applying database migrations for {ContextType}...", typeof(TContext).Name);
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying migrations");
            throw;
        }
    }
}