// DTOs for API contracts - separate from domain models (clean architecture)
// Demonstrates: Records for DTOs, required members, init-only setters
using System.ComponentModel.DataAnnotations;

namespace CloudBurger.Shared.Contracts;

/// <summary>
/// Request to create a new order.
/// Uses record for immutability and required members for compile-time safety.
/// </summary>
public sealed record CreateOrderRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public required string CustomerName { get; init; }

    [Required]
    public required string BurgerType { get; init; }

    [Range(1, 50)]
    public required int Quantity { get; init; }
}

/// <summary>
/// Response after creating an order.
/// </summary>
public sealed record CreateOrderResponse
{
    public required string OrderId { get; init; }
    public required string CustomerName { get; init; }
    public required string BurgerType { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal TotalPrice { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
}

/// <summary>
/// Order details in the public feed.
/// </summary>
public sealed record OrderDto
{
    public required string OrderId { get; init; }
    public required string CustomerName { get; init; }
    public required string BurgerType { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal TotalPrice { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
}

/// <summary>
/// Server-Sent Event message for real-time updates.
/// </summary>
public sealed record OrderCreatedEvent
{
    public required string EventType { get; init; }
    public required OrderDto Order { get; init; }
    public required string Id { get; init; }
}
