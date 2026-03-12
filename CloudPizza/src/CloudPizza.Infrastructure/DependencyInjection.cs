namespace CloudBurger.Infrastructure;

using CloudBurger.Infrastructure.Data;
using CloudBurger.Infrastructure.Notifications;
using CloudBurger.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for registering Infrastructure services.
/// Keeps service registration organized and follows separation of concerns.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add all Infrastructure services to the DI container.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool skipDbContext = false)
    {
        // Add EF Core with PostgreSQL (skip if already registered by Aspire)
        if (!skipDbContext)
        {
            services.AddDbContext<BurgerDbContext>((serviceProvider, options) =>
            {
                var connectionString = configuration.GetConnectionString("burgerdb")
                    ?? throw new InvalidOperationException("Database connection string 'burgerdb' not found");

                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                });

                // Enable sensitive data logging in development
                var environment = serviceProvider.GetService<IHostEnvironment>();
                if (environment?.IsDevelopment() == true)
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });
        }

        // Add background services
        services.AddHostedService<DatabaseInitializer>();
        services.AddSingleton<PostgresNotificationService>();
        services.AddHostedService(sp => sp.GetRequiredService<PostgresNotificationService>());

        // Add SSE manager
        services.AddSingleton<ServerSentEventsManager>();

        // Add QR code service
        services.AddSingleton<QrCodeService>();

        return services;
    }
}
