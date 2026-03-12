using CloudBurger.Shared.Contracts;
using CloudBurger.Shared.Domain;

namespace CloudBurger.Api.Features.Orders;

/// <summary>
/// Maps Order domain entities to API response DTOs.
/// Centralises all Order → DTO projection logic in one place.
/// Uses BurgerType extension members (GetDisplayName, GetPrice) for
/// consistent, source-generated display strings and live price lookups.
/// </summary>
public static class OrderMapper
{
    /// <summary>
    /// Maps a persisted Order to an OrderDto.
    /// GetDisplayName()  – source-generated via NetEscapades.EnumGenerators (fast, allocation-free)
    /// GetPrice()        – live lookup from BurgerTypeData.Info, so price changes
    ///                     are reflected without a re-migration.
    /// </summary>
    public static OrderDto ToDto(Order order) => new()
    {
        OrderId = order.Id.ToString(),
        CustomerName = order.CustomerName,
        BurgerType = order.BurgerType.GetDisplayName(),
        Quantity = order.Quantity,
        UnitPrice = order.BurgerType.GetPrice(),
        TotalPrice = order.BurgerType.GetPrice() * order.Quantity,
        CreatedAtUtc = order.CreatedAtUtc
    };

    /// <summary>
    /// Maps a persisted Order to a CreateOrderResponse.
    /// </summary>
    public static CreateOrderResponse ToCreateResponse(Order order) => new()
    {
        OrderId = order.Id.ToString(),
        CustomerName = order.CustomerName,
        BurgerType = order.BurgerType.GetDisplayName(),
        Quantity = order.Quantity,
        UnitPrice = order.BurgerType.GetPrice(),
        TotalPrice = order.BurgerType.GetPrice() * order.Quantity,
        CreatedAtUtc = order.CreatedAtUtc
    };
}
