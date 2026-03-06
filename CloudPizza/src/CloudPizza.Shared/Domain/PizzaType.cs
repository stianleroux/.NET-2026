// Pizza types as strongly-typed enum to avoid magic strings
namespace CloudPizza.Shared.Domain;

/// <summary>
/// Available pizza types in the system.
/// Using enum for type safety and clear domain vocabulary.
/// </summary>
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
/// Extension methods for PizzaType enum.
/// Demonstrates new .NET 10 extension members pattern.
/// </summary>
public static class PizzaTypeExtensions
{
    private static readonly Dictionary<PizzaType, (string Display, string Description, decimal Price)> PizzaInfo = new()
    {
        [PizzaType.Margherita] = ("Margherita", "Classic tomato and mozzarella", 12.99m),
        [PizzaType.Pepperoni] = ("Pepperoni", "Loaded with pepperoni", 14.99m),
        [PizzaType.Hawaiian] = ("Hawaiian", "Ham and pineapple", 13.99m),
        [PizzaType.Veggie] = ("Veggie Deluxe", "Fresh vegetables", 13.99m),
        [PizzaType.MeatLovers] = ("Meat Lovers", "All the meats", 16.99m),
        [PizzaType.BBQChicken] = ("BBQ Chicken", "Tangy BBQ sauce with chicken", 15.99m),
        [PizzaType.FourCheese] = ("Four Cheese", "Mozzarella, parmesan, gorgonzola, fontina", 14.99m),
        [PizzaType.Supreme] = ("Supreme", "Everything but the kitchen sink", 17.99m)
    };

    public static string GetDisplayName(this PizzaType pizzaType) =>
        PizzaInfo.TryGetValue(pizzaType, out var info) ? info.Display : pizzaType.ToString();

    public static string GetDescription(this PizzaType pizzaType) =>
        PizzaInfo.TryGetValue(pizzaType, out var info) ? info.Description : string.Empty;

    public static decimal GetPrice(this PizzaType pizzaType) =>
        PizzaInfo.TryGetValue(pizzaType, out var info) ? info.Price : 0m;
}
