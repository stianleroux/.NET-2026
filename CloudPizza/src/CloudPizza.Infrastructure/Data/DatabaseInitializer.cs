namespace CloudPizza.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Background service to initialize database schema and PostgreSQL triggers.
/// Runs at startup using IHostedService pattern.
/// </summary>
public sealed class DatabaseInitializer(
    IServiceProvider serviceProvider,
    ILogger<DatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing database...");

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PizzaDbContext>();

        try
        {
            // Apply migrations
            await dbContext.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations applied successfully");

            // Create the trigger function and trigger
            await CreateNotificationTrigger(dbContext, cancellationToken);
            logger.LogInformation("PostgreSQL NOTIFY trigger created successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    private static async Task CreateNotificationTrigger(PizzaDbContext dbContext, CancellationToken cancellationToken)
    {
        // Create the function
        var createFunction = @"
CREATE OR REPLACE FUNCTION notify_order_created()
RETURNS TRIGGER AS $$
DECLARE
    payload json;
BEGIN
    payload = json_build_object(
        'Id', NEW.id::text,
        'CustomerName', NEW.customer_name,
        'PizzaType', NEW.pizza_type,
        'Quantity', NEW.quantity,
        'TotalPrice', NEW.total_price,
        'CreatedAtUtc', NEW.created_at_utc
    );
    
    PERFORM pg_notify('orders_channel', payload::text);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;";

        await dbContext.Database.ExecuteSqlRawAsync(createFunction, cancellationToken);

        // Create the trigger
        var createTrigger = @"
DROP TRIGGER IF EXISTS order_created_trigger ON orders;
CREATE TRIGGER order_created_trigger
AFTER INSERT ON orders
FOR EACH ROW
EXECUTE FUNCTION notify_order_created();";

        await dbContext.Database.ExecuteSqlRawAsync(createTrigger, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Database initializer stopping");
        return Task.CompletedTask;
    }
}
