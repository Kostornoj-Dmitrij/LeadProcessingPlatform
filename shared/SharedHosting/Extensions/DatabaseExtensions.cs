using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using SharedHosting.Constants;

namespace SharedHosting.Extensions;

/// <summary>
/// Расширения для настройки базы данных
/// </summary>
public static class DatabaseExtensions
{
    public static IServiceCollection AddSharedDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? additionalOptions = null)
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(ConfigurationKeys.DefaultConnection);

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found");
        }

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<TContext>((sp, options) =>
        {
            options.UseNpgsql(dataSource, b => 
                    b.MigrationsAssembly(typeof(TContext).Assembly.FullName))
                .UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>());
            
            additionalOptions?.Invoke(options);
        });

        return services;
    }
}