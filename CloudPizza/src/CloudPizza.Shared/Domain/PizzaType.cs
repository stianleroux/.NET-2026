namespace CloudPizza.Shared.Domain;

using NetEscapades.EnumGenerators;

/// <summary>
/// Available pizza types in the system.
/// Using enum for type safety and clear domain vocabulary.
/// Uses NetEscapades.EnumGenerators for high-performance enum-to-string conversion.
/// Demonstrates C# 14 Extension Members for modern, intuitive API design.
/// </summary>
[EnumExtensions]
public enum PizzaType
{
    Margherita = 1,
    Pepperoni = 2,
    Hawaiian = 3,
    Veggie = 4,
    MeatLovers = 5,
    BBQChicken = 6,
    FourCheese = 7,
    Supreme = 8
}

/// <summary>
/// Pizza metadata shared across extension members.
/// Using file-scoped class for encapsulation.
/// </summary>
public static class PizzaTypeData
{
    public static readonly Dictionary<PizzaType, (string Description, decimal Price)> Info = new()
    {
        [PizzaType.Margherita] = ("Classic tomato and mozzarella", 12.99m),
        [PizzaType.Pepperoni] = ("Loaded with pepperoni", 14.99m),
        [PizzaType.Hawaiian] = ("Ham and pineapple", 13.99m),
        [PizzaType.Veggie] = ("Fresh vegetables", 13.99m),
        [PizzaType.MeatLovers] = ("All the meats", 16.99m),
        [PizzaType.BBQChicken] = ("Tangy BBQ sauce with chicken", 15.99m),
        [PizzaType.FourCheese] = ("Mozzarella, parmesan, gorgonzola, fontina", 14.99m),
        [PizzaType.Supreme] = ("Everything but the kitchen sink", 17.99m)
    };
}

/// <summary>
/// C# 14 Extension Members for PizzaType enum.
/// This modern syntax provides a cleaner, more intuitive API.
/// 
/// Benefits:
/// - No 'this' parameter in method signatures - written like instance methods
/// - 'this' keyword refers to the extended value
/// - Better IntelliSense and discoverability
/// - More object-oriented feel
/// 
/// Source-Generated Methods (by NetEscapades.EnumGenerators):
/// - ToStringFast() - High-performance enum-to-string conversion
/// - IsDefined() - Checks if a value is a defined enum member
/// - TryParse() - Fast enum parsing from string
/// - GetValues() - Returns all enum values
/// - GetNames() - Returns all enum names
/// </summary>
public static class PizzaTypeDataExtensions
{
    extension(PizzaType type)
    {
        /// <summary>
        /// Gets the display name of the pizza type.
        /// Uses source-generated ToStringFast() for optimal performance.
        /// </summary>
        public string GetDisplayName()
        {
            return type.ToStringFast();
        }

        /// <summary>
        /// Gets the description of the pizza type.
        /// </summary>
        public string GetDescription()
        {
            return PizzaTypeData.Info.TryGetValue(type, out var info) ? info.Description : string.Empty;
        }

        /// <summary>
        /// Gets the price of the pizza type.
        /// </summary>
        public decimal GetPrice()
        {
            return PizzaTypeData.Info.TryGetValue(type, out var info) ? info.Price : 0m;
        }
    }
}
