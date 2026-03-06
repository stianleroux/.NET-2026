// Domain model for Order entity
// Demonstrates: Rich domain model (not anemic), encapsulation, business rules
namespace CloudPizza.Shared.Domain;

/// <summary>
/// Order aggregate root with business logic and validation.
/// Demonstrates DDD principles with rich behavior, not anemic data bags.
/// </summary>
public sealed class Order
{
    // Private setters enforce encapsulation - use factory methods to create instances
    public OrderId Id { get; private set; }
    public required string CustomerName { get; init; }
    public required PizzaType PizzaType { get; init; }
    public required int Quantity { get; init; }
    public DateTime CreatedAtUtc { get; private set; }
    public decimal TotalPrice { get; private set; }

    // Private constructor forces use of factory methods
    private Order() { }

    /// <summary>
    /// Factory method to create a new order with business rules applied.
    /// </summary>
    public static Order Create(string customerName, PizzaType pizzaType, int quantity)
    {
        // Business rules enforcement
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName, nameof(customerName));
        
        if (customerName.Length < 2)
            throw new ArgumentException("Customer name must be at least 2 characters", nameof(customerName));
        
        if (customerName.Length > 100)
            throw new ArgumentException("Customer name cannot exceed 100 characters", nameof(customerName));
        
        if (quantity < 1)
            throw new ArgumentException("Quantity must be at least 1", nameof(quantity));
        
        if (quantity > 50)
            throw new ArgumentException("Cannot order more than 50 pizzas at once", nameof(quantity));
        
        if (!Enum.IsDefined(pizzaType))
            throw new ArgumentException("Invalid pizza type", nameof(pizzaType));

        var order = new Order
        {
            Id = OrderId.New(),
            CustomerName = customerName.Trim(),
            PizzaType = pizzaType,
            Quantity = quantity,
            CreatedAtUtc = DateTime.UtcNow,
            TotalPrice = pizzaType.GetPrice() * quantity
        };

        return order;
    }

    /// <summary>
    /// Reconstitute order from database (already validated).
    /// </summary>
    public static Order Reconstitute(OrderId id, string customerName, PizzaType pizzaType, int quantity, DateTime createdAtUtc)
    {
        return new Order
        {
            Id = id,
            CustomerName = customerName,
            PizzaType = pizzaType,
            Quantity = quantity,
            CreatedAtUtc = createdAtUtc,
            TotalPrice = pizzaType.GetPrice() * quantity
        };
    }
}
