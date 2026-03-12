
using CloudBurger.Shared.Common;

namespace CloudBurger.Shared.Domain;
/// <summary>
/// Order aggregate root with business logic and validation.
/// Demonstrates DDD principles with rich behavior, not anemic data bags.
/// </summary>
public sealed class Order
{
    // Private setters enforce encapsulation - use factory methods to create instances
    public OrderId Id { get; private set; }
    public required string CustomerName { get; init; }
    public required BurgerType BurgerType { get; init; }
    public required int Quantity { get; init; }
    public DateTime CreatedAtUtc { get; private set; }
    public decimal TotalPrice { get; private set; }

    // Private constructor forces use of factory methods
    private Order() { }

    /// <summary>
    /// Factory method to create a new order with business rules applied.
    /// Uses Result pattern with ValidationFailure for explicit error handling.
    /// Demonstrates: Pattern matching, functional error handling, structured validation
    /// </summary>
    public static Result<Order> Create(string customerName, BurgerType burgerType, int quantity)
    {
        // Validate customer name using pattern matching
        var nameValidation = customerName switch
        {
            null or "" => Result<string>.ValidationFailure(
                "Customer name validation failed",
                new Dictionary<string, string[]> { ["CustomerName"] = ["Customer name is required"] }),
            { Length: < 2 } => Result<string>.ValidationFailure(
                "Customer name validation failed",
                new Dictionary<string, string[]> { ["CustomerName"] = ["Customer name must be at least 2 characters"] }),
            { Length: > 100 } => Result<string>.ValidationFailure(
                "Customer name validation failed",
                new Dictionary<string, string[]> { ["CustomerName"] = ["Customer name cannot exceed 100 characters"] }),
            _ => Result<string>.Success(customerName.Trim())
        };

        if (nameValidation.IsFailure)
        {
            return Result<Order>.ValidationFailure(nameValidation.Error, nameValidation.ValidationErrors!);
        }

        // Validate quantity using pattern matching
        var quantityValidation = quantity switch
        {
            < 1 => Result<int>.ValidationFailure(
                "Quantity validation failed",
                new Dictionary<string, string[]> { ["Quantity"] = ["Quantity must be at least 1"] }),
            > 50 => Result<int>.ValidationFailure(
                "Quantity validation failed",
                new Dictionary<string, string[]> { ["Quantity"] = ["Cannot order more than 50 burgers at once"] }),
            _ => Result<int>.Success(quantity)
        };

        if (quantityValidation.IsFailure)
        {
            return Result<Order>.ValidationFailure(quantityValidation.Error, quantityValidation.ValidationErrors!);
        }

        // Validate burger type using pattern matching
        var burgerTypeValidation = Enum.IsDefined(burgerType) switch
        {
            false => Result<BurgerType>.ValidationFailure(
                "Burger type validation failed",
                new Dictionary<string, string[]> { ["BurgerType"] = [$"Invalid burger type: {burgerType}"] }),
            true => Result<BurgerType>.Success(burgerType)
        };

        if (burgerTypeValidation.IsFailure)
        {
            return Result<Order>.ValidationFailure(burgerTypeValidation.Error, burgerTypeValidation.ValidationErrors!);
        }

        // All validations passed - create the order
        var order = new Order
        {
            Id = OrderId.New(),
            CustomerName = nameValidation.Value,
            BurgerType = burgerType,
            Quantity = quantity,
            CreatedAtUtc = DateTime.UtcNow,
            TotalPrice = burgerType.GetPrice() * quantity
        };

        return Result<Order>.Success(order);
    }

    /// <summary>
    /// Reconstitute order from database (already validated).
    /// </summary>
    public static Order Reconstitute(OrderId id, string customerName, BurgerType burgerType, int quantity, DateTime createdAtUtc)
    {
        return new Order
        {
            Id = id,
            CustomerName = customerName,
            BurgerType = burgerType,
            Quantity = quantity,
            CreatedAtUtc = createdAtUtc,
            TotalPrice = burgerType.GetPrice() * quantity
        };
    }
}
