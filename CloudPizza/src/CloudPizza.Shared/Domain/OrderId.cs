// Strongly-typed ID for Orders - prevents primitive obsession
// Demonstrates: Records, required members, value object pattern, FluentValidation
namespace CloudPizza.Shared.Domain;

using CloudPizza.Shared.Common;
using FluentValidation;

/// <summary>
/// Strongly-typed Order identifier to prevent mixing with other GUIDs.
/// Uses record for value-based equality and immutability.
/// Demonstrates: FluentValidation, Result pattern, functional validation
/// </summary>
public readonly record struct OrderId
{
    public Guid Value { get; init; }

    // Private constructor - use factory methods that return Result<OrderId>
    private OrderId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new OrderId with validation using Result pattern.
    /// </summary>
    public static Result<OrderId> Create(Guid value)
    {
        var validator = new OrderIdValidator();
        var validationResult = validator.Validate(value);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return Result<OrderId>.ValidationFailure(
                "OrderId validation failed",
                errors
            );
        }

        return Result<OrderId>.Success(new OrderId(value));
    }

    /// <summary>
    /// Creates a new OrderId with a fresh GUID.
    /// This always succeeds as Guid.NewGuid() never returns empty.
    /// </summary>
    public static OrderId New()
    {
        return new(Guid.NewGuid());
    }

    /// <summary>
    /// Parses a string into an OrderId using Result pattern.
    /// </summary>
    public static Result<OrderId> Parse(string value)
    {
        if (!Guid.TryParse(value, out var guid))
        {
            return Result<OrderId>.ValidationFailure(
                "Invalid OrderId format",
                new Dictionary<string, string[]>
                {
                    ["Value"] = ["The value must be a valid GUID format"]
                }
            );
        }

        return Create(guid);
    }

    /// <summary>
    /// Attempts to parse a string into an OrderId.
    /// Returns true if successful, false otherwise.
    /// </summary>
    public static bool TryParse(string? value, out OrderId orderId)
    {
        if (Guid.TryParse(value, out var guid) && guid != Guid.Empty)
        {
            orderId = new OrderId(guid);
            return true;
        }

        orderId = default;
        return false;
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    // Implicit conversion for convenience
    public static implicit operator Guid(OrderId id)
    {
        return id.Value;
    }
}

/// <summary>
/// FluentValidation validator for OrderId.
/// Demonstrates: Declarative validation rules, fluent API
/// </summary>
public sealed class OrderIdValidator : AbstractValidator<Guid>
{
    public OrderIdValidator()
    {
        RuleFor(guid => guid)
            .NotEmpty()
            .WithMessage("OrderId cannot be empty")
            .WithName("Value");

        RuleFor(guid => guid)
            .Must(guid => guid != Guid.Empty)
            .WithMessage("OrderId must be a non-empty GUID")
            .WithName("Value");
    }
}
