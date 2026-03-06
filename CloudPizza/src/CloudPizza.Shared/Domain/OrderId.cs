// Strongly-typed ID for Orders - prevents primitive obsession
// Demonstrates: Records, required members, value object pattern
namespace CloudPizza.Shared.Domain;

/// <summary>
/// Strongly-typed Order identifier to prevent mixing with other GUIDs.
/// Uses record for value-based equality and immutability.
/// </summary>
public readonly record struct OrderId
{
    public Guid Value { get; init; }

    public OrderId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("OrderId cannot be empty", nameof(value));
        
        Value = value;
    }

    public static OrderId New() => new(Guid.NewGuid());
    
    public static OrderId Parse(string value) => new(Guid.Parse(value));
    
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

    public override string ToString() => Value.ToString();
    
    // Implicit conversion for convenience
    public static implicit operator Guid(OrderId id) => id.Value;
}
