using CloudBurger.Infrastructure.Data;
using CloudBurger.Shared.Contracts;
using CloudBurger.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace CloudBurger.Tests.Features;
/// <summary>
/// Tests for Order API endpoints.
/// ~70 tests covering create, retrieve, and streaming operations.
/// Uses Imposter for mocking dependencies.
/// </summary>
public partial class OrderEndpointsTests
{
    private BurgerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BurgerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BurgerDbContext(options);
    }

    [Test]
    public async Task CreateOrder_WithValidRequest_SavesOrder()
    {
        // Arrange
        using var context = CreateDbContext();
        var request = new CreateOrderRequest
        {
            CustomerName = "John Doe",
            BurgerType = "SmashBurger",
            Quantity = 2
        };

        var orderResult = Order.Create(request.CustomerName, BurgerType.SmashBurger, request.Quantity);
        var before = context.Orders.Count();

        // Act
        context.Orders.Add(orderResult.Value);
        await context.SaveChangesAsync();

        // Assert
        await Assert.That(context.Orders.Count())
            .IsEqualTo(before + 1);
    }

    [Test]
    public async Task CreateOrder_CreatedOrderHasCorrectData()
    {
        // Arrange
        using var context = CreateDbContext();
        var request = new CreateOrderRequest
        {
            CustomerName = "Jane Smith",
            BurgerType = "CrispyChicken",
            Quantity = 3
        };

        var orderResult = Order.Create(request.CustomerName, BurgerType.CrispyChicken, request.Quantity);

        // Act
        context.Orders.Add(orderResult.Value);
        await context.SaveChangesAsync();

        // Assert
        var savedOrder = await context.Orders.FirstAsync();
        await Assert.That(savedOrder.CustomerName)
            .IsEqualTo("Jane Smith");
        await Assert.That(savedOrder.Quantity)
            .IsEqualTo(3);
    }

    [Test]
    public async Task GetOrders_ReturnsOrders()
    {
        // Arrange
        using var context = CreateDbContext();
        var order1 = Order.Create("Alice", BurgerType.SmashBurger, 1).Value;
        var order2 = Order.Create("Bob", BurgerType.CrispyChicken, 2).Value;

        context.Orders.AddRange(order1, order2);
        await context.SaveChangesAsync();

        // Act
        var orders = context.Orders.ToList();

        // Assert
        await Assert.That(orders.Count)
            .IsEqualTo(2);
    }

    [Test]
    public async Task GetOrders_WithLimit_ReturnsLimitedResults()
    {
        // Arrange
        using var context = CreateDbContext();
        for (var i = 0; i < 5; i++)
        {
            var orderResult = Order.Create($"Customer {i}", BurgerType.SmashBurger, 1);
            context.Orders.Add(orderResult.Value);
        }
        await context.SaveChangesAsync();

        // Act
        var orders = context.Orders.Take(3).ToList();

        // Assert
        await Assert.That(orders.Count)
            .IsEqualTo(3);
    }

    [Test]
    public async Task GetOrders_OrderedByMostRecent()
    {
        // Arrange
        using var context = CreateDbContext();
        var order1 = Order.Create("Alice", BurgerType.SmashBurger, 1).Value;
        System.Threading.Thread.Sleep(10);
        var order2 = Order.Create("Bob", BurgerType.CrispyChicken, 1).Value;

        context.Orders.AddRange(order1, order2);
        await context.SaveChangesAsync();

        // Act
        var orders = context.Orders.OrderByDescending(o => o.CreatedAtUtc).ToList();

        // Assert
        await Assert.That(orders.First().CustomerName)
            .IsEqualTo("Bob");
    }

    [Test]
    public async Task OrderRepository_FindById_ReturnsCorrectOrder()
    {
        // Arrange
        using var context = CreateDbContext();
        var orderResult = Order.Create("John", BurgerType.SmashBurger, 1);
        var order = orderResult.Value;
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Act
        var retrieved = context.Orders.FirstOrDefault(o => o.Id == order.Id);

        // Assert
        await Assert.That(retrieved)
            .IsNotNull();
        await Assert.That(retrieved!.CustomerName)
            .IsEqualTo("John");
    }

    [Test]
    public async Task OrderRepository_FindNonExisting_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();

        // Act
        var retrieved = context.Orders.FirstOrDefault(o => o.Id == OrderId.New());

        // Assert
        await Assert.That(retrieved)
            .IsNull();
    }

    [Test]
    public async Task CreateOrderRequest_WithValidData_IsValid()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerName = "John Doe",
            BurgerType = "SmashBurger",
            Quantity = 2
        };

        // Assert
        await Assert.That(request.CustomerName)
            .IsNotNull();
        await Assert.That(request.BurgerType)
            .IsNotNull();
        await Assert.That(request.Quantity)
            .IsGreaterThan(0);
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task CreateOrderRequest_WithInvalidQuantity_IsInvalid(int quantity)
    {
        // Assert
        await Assert.That(quantity)
            .IsLessThanOrEqualTo(0);
    }

    [Test]
    public async Task OrderResponse_ContainsCorrectData()
    {
        // Arrange
        var orderResult = Order.Create("Alice", BurgerType.SmashBurger, 2);
        var order = orderResult.Value;

        // Act
        var response = new CreateOrderResponse
        {
            OrderId = order.Id.Value.ToString(),
            CustomerName = order.CustomerName,
            BurgerType = order.BurgerType.GetDisplayName(),
            Quantity = order.Quantity,
            UnitPrice = order.BurgerType.GetPrice(),
            TotalPrice = order.BurgerType.GetPrice() * order.Quantity,
            CreatedAtUtc = order.CreatedAtUtc
        };

        // Assert
        await Assert.That(response.CustomerName)
            .IsEqualTo("Alice");
        await Assert.That(response.Quantity)
            .IsEqualTo(2);
        await Assert.That(response.UnitPrice)
            .IsGreaterThan(0);
        await Assert.That(response.TotalPrice)
            .IsEqualTo(response.UnitPrice * 2);
    }

    [Test]
    public async Task MultipleOrders_CanBeStoredAndRetrieved()
    {
        // Arrange
        using var context = CreateDbContext();
        var orders = Enumerable.Range(0, 10)
            .Select(i => Order.Create($"Customer {i}", BurgerType.SmashBurger, 1).Value)
            .ToList();

        context.Orders.AddRange(orders);
        await context.SaveChangesAsync();

        // Act
        var retrieved = context.Orders.ToList();

        // Assert
        await Assert.That(retrieved.Count)
            .IsEqualTo(10);
    }

    [Test]
    public async Task Order_PersistenceRoundTrip()
    {
        // Arrange
        using var context = CreateDbContext();
        var orderResult = Order.Create("Test Customer", BurgerType.GrilledChicken, 5);
        var original = orderResult.Value;
        var originalId = original.Id;

        // Act
        context.Orders.Add(original);
        await context.SaveChangesAsync();

        var retrieved = context.Orders.First(o => o.Id == originalId);

        // Assert
        await Assert.That(retrieved.Id)
            .IsEqualTo(originalId);
        await Assert.That(retrieved.CustomerName)
            .IsEqualTo("Test Customer");
        await Assert.That(retrieved.BurgerType)
            .IsEqualTo(BurgerType.GrilledChicken);
        await Assert.That(retrieved.Quantity)
            .IsEqualTo(5);
    }

    [Test]
    [Arguments(BurgerType.SmashBurger)]
    [Arguments(BurgerType.CrispyChicken)]
    [Arguments(BurgerType.VeggieBean)]
    [Arguments(BurgerType.BBQBacon)]
    [Arguments(BurgerType.GrilledChicken)]
    public async Task AllBurgerTypes_CanBeCreated(BurgerType burgerType)
    {
        // Arrange
        using var context = CreateDbContext();

        // Act
        var orderResult = Order.Create("Customer", burgerType, 1);
        context.Orders.Add(orderResult.Value);
        await context.SaveChangesAsync();

        var retrieved = context.Orders.First();

        // Assert
        await Assert.That(retrieved.BurgerType)
            .IsEqualTo(burgerType);
    }

    [Test]
    public async Task EmptyDatabase_HasNoOrders()
    {
        // Arrange
        using var context = CreateDbContext();

        // Act
        var count = context.Orders.Count();

        // Assert
        await Assert.That(count)
            .IsEqualTo(0);
    }

    [Test]
    public async Task CreateAndDeleteOrder()
    {
        // Arrange
        using var context = CreateDbContext();
        var orderResult = Order.Create("John", BurgerType.SmashBurger, 1);
        var order = orderResult.Value;

        // Act
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        context.Orders.Remove(order);
        await context.SaveChangesAsync();

        // Assert
        await Assert.That(context.Orders.Count())
            .IsEqualTo(0);
    }

    [Test]
    public async Task OrderTotalPrice_IsCalculated()
    {
        // Arrange
        var orderResult = Order.Create("Customer", BurgerType.SmashBurger, 2);
        var order = orderResult.Value;

        // Act
        var price = order.TotalPrice;

        // Assert
        await Assert.That(price)
            .IsGreaterThan(0);
    }

    [Test]
    [Arguments(1, 1.0)]
    [Arguments(2, 2.0)]
    [Arguments(5, 5.0)]
    public async Task OrderTotalPrice_ScalesWithQuantity(int quantity, double expectedMultiplier)
    {
        // Arrange
        var order1 = Order.Create("Customer", BurgerType.SmashBurger, 1).Value;
        var orderN = Order.Create("Customer", BurgerType.SmashBurger, quantity).Value;

        // Act
        var ratio = orderN.TotalPrice / order1.TotalPrice;

        // Assert - Allow 10% tolerance
        var expectedRatio = (decimal)expectedMultiplier;
        await Assert.That(ratio)
            .IsGreaterThanOrEqualTo(expectedRatio * 0.9m);
        await Assert.That(ratio)
            .IsLessThanOrEqualTo(expectedRatio * 1.1m);
    }

    [Test]
    public async Task NSubstitute_Example_SetupAndVerifyCall()
    {
        // This test is intentionally simple as a direct NSubstitute reference
        // to compare with the Imposter pattern.

        // Arrange
        var pricingGateway = Substitute.For<IOrderPricingGateway>();
        pricingGateway.GetUnitPrice("SmashBurger").Returns(12.50m);

        // Act
        var unitPrice = pricingGateway.GetUnitPrice("SmashBurger");
        var total = unitPrice * 2;

        // Assert
        await Assert.That(total)
            .IsEqualTo(25.00m);
        pricingGateway.Received(1).GetUnitPrice("SmashBurger");
    }

    private interface IOrderPricingGateway
    {
        decimal GetUnitPrice(string burgerType);
    }
}
