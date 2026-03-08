namespace CloudPizza.Infrastructure.Notifications;

using System.Collections.Concurrent;
using System.Threading.Channels;
using CloudPizza.Shared.Contracts;
using Microsoft.Extensions.Logging;

/// <summary>
/// Manages Server-Sent Events connections and broadcasts order updates.
/// Uses channels for efficient async message distribution.
/// </summary>
public sealed class ServerSentEventsManager(ILogger<ServerSentEventsManager> logger)
{
    private readonly ConcurrentDictionary<string, Channel<OrderDto>> connections = new();

    /// <summary>
    /// Register a new SSE client and return a stream of order updates.
    /// Each client gets their own channel for isolated message delivery.
    /// </summary>
    public async IAsyncEnumerable<OrderDto> RegisterClientAsync(string clientId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<OrderDto>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        if (!connections.TryAdd(clientId, channel))
        {
            logger.LogWarning("Client {ClientId} already registered", clientId);
            yield break;
        }

        logger.LogInformation("Client {ClientId} connected. Total connections: {Count}", clientId, connections.Count);

        try
        {
            await foreach (var order in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return order;
            }
        }
        finally
        {
            UnregisterClient(clientId);
        }
    }

    /// <summary>
    /// Broadcast an order to all connected clients.
    /// Uses channels for non-blocking, concurrent message delivery.
    /// </summary>
    public async ValueTask BroadcastOrderAsync(OrderDto order, CancellationToken cancellationToken = default)
    {
        if (connections.IsEmpty)
        {
            logger.LogDebug("No connected clients to broadcast to");
            return;
        }

        logger.LogInformation("Broadcasting order {OrderId} to {Count} clients", order.OrderId, connections.Count);

        var tasks = connections.Values.Select(async channel =>
        {
            try
            {
                await channel.Writer.WriteAsync(order, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error broadcasting to client");
            }
        });

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Remove a disconnected client.
    /// </summary>
    private void UnregisterClient(string clientId)
    {
        if (connections.TryRemove(clientId, out var channel))
        {
            channel.Writer.Complete();
            logger.LogInformation("Client {ClientId} disconnected. Remaining connections: {Count}", clientId, connections.Count);
        }
    }

    /// <summary>
    /// Get the current number of connected clients.
    /// </summary>
    public int GetConnectionCount()
    {
        return connections.Count;
    }
}
