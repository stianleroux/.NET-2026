// PostgreSQL LISTEN/NOTIFY service for real-time change detection
// Demonstrates: IHostedService, async streams, channel-based communication
using System.Text.Json;
using System.Threading.Channels;
using CloudPizza.Shared.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CloudPizza.Infrastructure.Notifications;

/// <summary>
/// Background service that listens to PostgreSQL NOTIFY events
/// and broadcasts them to connected clients via channels.
/// Uses PostgreSQL's LISTEN/NOTIFY for efficient change detection.
/// </summary>
public sealed class PostgresNotificationService(
    IConfiguration configuration,
    ILogger<PostgresNotificationService> logger) : BackgroundService
{
    private const string ChannelName = "orders_channel";
    private readonly Channel<OrderDto> _channel = Channel.CreateUnbounded<OrderDto>(new UnboundedChannelOptions
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
        await foreach (var order in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return order;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PostgreSQL LISTEN service starting...");

        var connectionString = configuration.GetConnectionString("pizzadb")
            ?? throw new InvalidOperationException("Database connection string 'pizzadb' not found");

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
                    logger.LogDebug("Received notification from channel '{Channel}': {Payload}", 
                        args.Channel, args.Payload);

                    // Parse the JSON payload from PostgreSQL trigger
                    var orderData = JsonSerializer.Deserialize<OrderNotificationPayload>(args.Payload);
                    
                    if (orderData is not null)
                    {
                        var orderDto = new OrderDto
                        {
                            OrderId = orderData.Id,
                            CustomerName = orderData.CustomerName,
                            PizzaType = orderData.PizzaType,
                            Quantity = orderData.Quantity,
                            TotalPrice = orderData.TotalPrice,
                            CreatedAtUtc = orderData.CreatedAtUtc
                        };

                        await _channel.Writer.WriteAsync(orderDto, stoppingToken);
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
            _channel.Writer.Complete();
            logger.LogInformation("PostgreSQL LISTEN service stopped");
        }
    }

    // Internal model for deserializing PostgreSQL JSON payload
    private sealed record OrderNotificationPayload
    {
        public required string Id { get; init; }
        public required string CustomerName { get; init; }
        public required string PizzaType { get; init; }
        public required int Quantity { get; init; }
        public required decimal TotalPrice { get; init; }
        public required DateTime CreatedAtUtc { get; init; }
    }
}
