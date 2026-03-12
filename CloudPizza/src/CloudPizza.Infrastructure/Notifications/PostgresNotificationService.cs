namespace CloudBurger.Infrastructure.Notifications;

using System.Text.Json;
using System.Threading.Channels;
using CloudBurger.Shared.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

/// <summary>
/// Background service that listens to PostgreSQL NOTIFY events
/// and broadcasts them to connected clients via channels.
/// Uses PostgreSQL's LISTEN/NOTIFY for efficient change detection.
/// </summary>
public sealed partial class PostgresNotificationService(IConfiguration configuration, ILogger<PostgresNotificationService> logger) : BackgroundService
{
    private const string ChannelName = "orders_channel";
    private readonly Channel<OrderDto> channel = Channel.CreateUnbounded<OrderDto>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = true
    });

    /// <summary>
    /// Subscribe to order notifications as an async stream.
    /// Demonstrates IAsyncEnumerable for streaming data.
    /// </summary>
    public async IAsyncEnumerable<OrderDto> SubscribeAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var order in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return order;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PostgreSQL LISTEN service starting...");

        var connectionString = configuration.GetConnectionString("burgerdb")
            ?? throw new InvalidOperationException("Database connection string 'burgerdb' not found");

        await using var connection = new NpgsqlConnection(connectionString);

        try
        {
            await connection.OpenAsync(stoppingToken);
            logger.LogInformation("Connected to PostgreSQL for LISTEN");

            // Set up notification handler
            connection.Notification += async (sender, args) =>
            {
                try
                {
                    logger.LogDebug("Received notification from channel '{Channel}': {Payload}", args.Channel, args.Payload);

                    // Parse the JSON payload from PostgreSQL trigger
                    var orderData = JsonSerializer.Deserialize<OrderNotificationPayload>(args.Payload);

                    if (orderData is not null)
                    {
                        var orderDto = new OrderDto
                        {
                            OrderId = orderData.Id,
                            CustomerName = orderData.CustomerName,
                            BurgerType = orderData.BurgerType,
                            Quantity = orderData.Quantity,
                            TotalPrice = orderData.TotalPrice,
                            CreatedAtUtc = orderData.CreatedAtUtc
                        };

                        await channel.Writer.WriteAsync(orderDto, stoppingToken);
                        logger.LogInformation("Order {OrderId} notification broadcasted", orderData.Id);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing notification");
                }
            };

            // Start listening to the channel
            await using var cmd = new NpgsqlCommand($"LISTEN {ChannelName}", connection);
            await cmd.ExecuteNonQueryAsync(stoppingToken);
            logger.LogInformation("Listening to PostgreSQL channel '{Channel}'", ChannelName);

            // Keep connection alive and wait for notifications
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait for notifications (this is non-blocking)
                await connection.WaitAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("PostgreSQL LISTEN service is stopping");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in PostgreSQL LISTEN service");
            throw;
        }
        finally
        {
            channel.Writer.Complete();
            logger.LogInformation("PostgreSQL LISTEN service stopped");
        }
    }
}
