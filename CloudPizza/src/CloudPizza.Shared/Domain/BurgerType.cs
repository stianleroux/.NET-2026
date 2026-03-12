namespace CloudBurger.Shared.Domain;

using NetEscapades.EnumGenerators;

/// <summary>
/// Available burger types in the system.
/// Using enum for type safety and clear domain vocabulary.
/// Uses NetEscapades.EnumGenerators for high-performance enum-to-string conversion.
/// Demonstrates C# 14 Extension Members for modern, intuitive API design.
/// </summary>
[EnumExtensions]
public enum BurgerType
{
    SmashBurger = 1,
    CrispyChicken = 2,
    BBQBacon = 3,
    VeggieBean = 4,
    DoubleBeef = 5,
    GrilledChicken = 6,
    SwissMushroom = 7,
    ClassicCheese = 8
}

/// <summary>
/// Burger metadata shared across extension members.
/// Using file-scoped class for encapsulation.
/// </summary>
public static class BurgerTypeData
{
    public static readonly Dictionary<BurgerType, (string Description, decimal Price)> Info = new()
    {
        [BurgerType.SmashBurger] = ("Smashed beef patty, cheddar, pickles, and burger sauce", 12.99m),
        [BurgerType.CrispyChicken] = ("Crispy chicken fillet with lettuce and spicy mayo", 13.99m),
        [BurgerType.BBQBacon] = ("Beef patty with smoky BBQ sauce, bacon, and onion", 14.99m),
        [BurgerType.VeggieBean] = ("Black bean patty with lettuce, tomato, and avocado", 12.49m),
        [BurgerType.DoubleBeef] = ("Two beef patties with double cheese and house sauce", 16.49m),
        [BurgerType.GrilledChicken] = ("Grilled chicken breast with herb mayo and greens", 13.49m),
        [BurgerType.SwissMushroom] = ("Beef patty with sautéed mushrooms and Swiss cheese", 14.49m),
        [BurgerType.ClassicCheese] = ("Classic beef burger with cheddar, onion, and ketchup", 11.99m)
    };
}

/// <summary>
/// C# 14 Extension Members for BurgerType enum.
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
public static class BurgerTypeDataExtensions
{
    extension(BurgerType type)
    {
        /// <summary>
        /// Gets the display name of the burger type.
        /// Uses source-generated ToStringFast() for optimal performance.
        /// </summary>
        public string GetDisplayName()
        {
            return type.ToStringFast();
        }

        /// <summary>
        /// Gets the description of the burger type.
        /// </summary>
        public string GetDescription()
        {
            return BurgerTypeData.Info.TryGetValue(type, out var info) ? info.Description : string.Empty;
        }

        /// <summary>
        /// Gets the price of the burger type.
        /// </summary>
        public decimal GetPrice()
        {
            return BurgerTypeData.Info.TryGetValue(type, out var info) ? info.Price : 0m;
        }
    }
}
