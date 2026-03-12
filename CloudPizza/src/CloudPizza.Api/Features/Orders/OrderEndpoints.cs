// Order endpoints using Minimal API route groups
// Demonstrates: Route groups, typed results, validation, Result pattern, SSE
using System.Text.Json;
using CloudBurger.Infrastructure.Data;
using CloudBurger.Infrastructure.Notifications;
using CloudBurger.Shared.Contracts;
using CloudBurger.Shared.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Results.Models;

namespace CloudBurger.Api.Features.Orders;

/// <summary>
/// Extension methods to register order-related endpoints.
/// Feature-based organization keeps related functionality together.
/// </summary>
public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this RouteGroupBuilder group)
    {
        var orders = group.MapGroup("/orders")
            .WithTags("Orders");

        // POST /api/orders - Create new order
        orders.MapPost("/", CreateOrderAsync)
            .WithName("CreateOrder")
            .WithSummary("Create a new burger order")
            .WithDescription("Creates a new burger order with validation. Returns the created order details.")
            .Produces<CreateOrderResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // GET /api/orders - Get recent orders
        orders.MapGet("/", GetOrdersAsync)
            .WithName("GetOrders")
            .WithSummary("Get recent orders")
            .WithDescription("Retrieves the most recent burger orders.")
            .Produces<List<OrderDto>>(StatusCodes.Status200OK);

        // GET /api/orders/stream - SSE stream of new orders
        orders.MapGet("/stream", StreamOrdersAsync)
            .WithName("StreamOrders")
            .WithSummary("Stream new orders in real-time")
            .WithDescription("Server-Sent Events endpoint that streams new orders as they are created.")
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
            .ExcludeFromDescription(); // Don't show in OpenAPI (SSE not well supported)

        return group;
    }

    /// <summary>
    /// Create a new burger order with validation and Result pattern.
    /// Demonstrates: Typed results, validation, Result pattern, strongly-typed IDs.
    /// </summary>
    private static async Task<Results<Created<CreateOrderResponse>, ValidationProblem, ProblemHttpResult>> CreateOrderAsync(
        CreateOrderRequest request,
        BurgerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Parse burger type
        if (!Enum.TryParse<BurgerType>(request.BurgerType, ignoreCase: true, out var burgerType))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["BurgerType"] = [$"Invalid burger type. Valid values: {string.Join(", ", Enum.GetNames<BurgerType>())}"]
            });
        }

        // Create domain entity (enforces business rules) using Result pattern with ValidationFailure
        var orderResult = Order.Create(request.CustomerName, burgerType, request.Quantity);

        // Handle validation failures using pattern matching
        if (orderResult.IsFailure)
        {
            // If ValidationErrors dictionary exists, use it; otherwise create one from Error message
            var validationErrors = orderResult.ValidationErrors ?? new Dictionary<string, string[]>
            {
                ["Order"] = [orderResult.Error]
            };

            return TypedResults.ValidationProblem(validationErrors);
        }

        var order = orderResult.Value;

        // Persist to database
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new CreateOrderResponse
        {
            OrderId = order.Id.ToString(),
            CustomerName = order.CustomerName,
            BurgerType = order.BurgerType.GetDisplayName(),
            Quantity = order.Quantity,
            TotalPrice = order.TotalPrice,
            CreatedAtUtc = order.CreatedAtUtc
        };

        return TypedResults.Created($"/api/orders/{order.Id}", response);
    }

    /// <summary>
    /// Get recent orders from the database.
    /// Demonstrates: Async queries, projection, mapping.
    /// </summary>
    private static async Task<Ok<List<OrderDto>>> GetOrdersAsync(
        BurgerDbContext dbContext,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var orders = await dbContext.Orders
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(Math.Min(limit, 100)) // Cap at 100
            .Select(o => new OrderDto
            {
                OrderId = o.Id.ToString(),
                CustomerName = o.CustomerName,
                BurgerType = o.BurgerType.ToString(),
                Quantity = o.Quantity,
                TotalPrice = o.TotalPrice,
                CreatedAtUtc = o.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(orders);
    }

    /// <summary>
    /// Server-Sent Events endpoint for real-time order updates.
    /// Demonstrates: SSE, async streams, long-lived connections.
    /// </summary>
    private static async Task StreamOrdersAsync(
        HttpContext context,
        PostgresNotificationService notificationService)
    {
        var response = context.Response;
        response.Headers.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers.Connection = "keep-alive";

        var clientId = Guid.NewGuid().ToString();

        try
        {
            // Subscribe to PostgreSQL notifications
            await foreach (var order in notificationService.SubscribeAsync(context.RequestAborted))
            {
                // Format as SSE message
                var json = JsonSerializer.Serialize(new OrderCreatedEvent
                {
                    EventType = "order-created",
                    Order = order,
                    Id = Guid.NewGuid().ToString()
                });

                var sseMessage = $"event: order-created\ndata: {json}\nid: {DateTime.UtcNow.Ticks}\n\n";
                
                await response.WriteAsync(sseMessage, context.RequestAborted);
                await response.Body.FlushAsync(context.RequestAborted);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected - this is normal
        }
    }
}
