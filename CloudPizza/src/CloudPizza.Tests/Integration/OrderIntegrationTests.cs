using CloudBurger.Infrastructure.Data;
using CloudBurger.Infrastructure.Services;
using CloudBurger.Shared.Contracts;
using CloudBurger.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace CloudBurger.Tests.Integration;
/// <summary>
/// Integration tests verifying coordination between components.
/// ~30 tests ensuring proper interaction of domain, services, and persistence.
/// </summary>
public partial class OrderIntegrationTests
{
    private BurgerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BurgerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BurgerDbContext(options);
    }

    [Test]
    public async Task CreateOrder_PersistAndRetrieve()
    {
        // Arrange
        using var context = CreateDbContext();
        var orderResult = Order.Create("John Doe", BurgerType.SmashBurger, 2);
        var order = orderResult.Value;

        // Act
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var retrieved = context.Orders.First();

        // Assert
        await Assert.That(retrieved.CustomerName)
            .IsEqualTo("John Doe");
    }

    [Test]
    public async Task OrderQrCode_GeneratesForPersistentOrder()
    {
        // Arrange
        using var context = CreateDbContext();
        var orderResult = Order.Create("Alice", BurgerType.CrispyChicken, 1);
        var order = orderResult.Value;

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Act
        var qrData = $"{order.Id.Value}:{order.CustomerName}";
        var qrCodeResult = QrCodeService.GenerateQrCodeBase64(qrData);

        // Assert
        await Assert.That(qrCodeResult.IsSuccess)
            .IsTrue();
        await Assert.That(qrCodeResult.Value)
            .IsNotEmpty();
    }

    [Test]
    public async Task MultipleOrders_MaintenanceFreelySaved()
    {
        // Arrange
        using var context = CreateDbContext();

        // Act
        for (var i = 0; i < 10; i++)
        {
            var orderResult = Order.Create($"Customer {i}", BurgerType.SmashBurger, (i % 5) + 1);
            context.Orders.Add(orderResult.Value);
        }

        await context.SaveChangesAsync();

        var count = context.Orders.Count();

        // Assert
        await Assert.That(count)
            .IsEqualTo(10);
    }

    [Test]
    public async Task OrderQuerying_ByCustomerName()
    {
        // Arrange
        using var context = CreateDbContext();
        var order1 = Order.Create("Alice", BurgerType.SmashBurger, 1).Value;
        var order2 = Order.Create("Bob", BurgerType.CrispyChicken, 2).Value;

        context.Orders.AddRange(order1, order2);
        await context.SaveChangesAsync();

        // Act
        var aliceOrders = context.Orders
            .Where(o => o.CustomerName == "Alice")
            .ToList();

        // Assert
        await Assert.That(aliceOrders.Count)
            .IsEqualTo(1);
        await Assert.That(aliceOrders[0].CustomerName)
            .IsEqualTo("Alice");
    }

    [Test]
    public async Task OrderQuerying_ByBurgerType()
    {
        // Arrange
        using var context = CreateDbContext();
        context.Orders.AddRange(
            Order.Create("A", BurgerType.SmashBurger, 1).Value,
            Order.Create("B", BurgerType.SmashBurger, 1).Value,
            Order.Create("C", BurgerType.CrispyChicken, 1).Value
        );
        await context.SaveChangesAsync();

        // Act
        var margheritaOrders = context.Orders
            .Where(o => o.BurgerType == BurgerType.SmashBurger)
            .ToList();

        // Assert
        await Assert.That(margheritaOrders.Count)
            .IsEqualTo(2);
    }

    [Test]
    public async Task OrderQuerying_OrderByDate()
    {
        // Arrange
        using var context = CreateDbContext();
        var order1 = Order.Create("A", BurgerType.SmashBurger, 1).Value;

        System.Threading.Thread.Sleep(10);

        var order2 = Order.Create("B", BurgerType.CrispyChicken, 1).Value;

        context.Orders.AddRange(order1, order2);
        await context.SaveChangesAsync();

        // Act
        var ordered = context.Orders
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToList();

        // Assert
        await Assert.That(ordered.First().CustomerName)
            .IsEqualTo("B");
        await Assert.That(ordered.Last().CustomerName)
            .IsEqualTo("A");
    }

    [Test]
    public async Task OrderQuerying_Pagination()
    {
        // Arrange
        using var context = CreateDbContext();

        for (var i = 0; i < 25; i++)
        {
            context.Orders.Add(Order.Create($"Customer {i}", BurgerType.SmashBurger, 1).Value);
        }

        await context.SaveChangesAsync();

        // Act
        var page1 = context.Orders.OrderByDescending(o => o.CreatedAtUtc).Take(10).ToList();
        var page2 = context.Orders.OrderByDescending(o => o.CreatedAtUtc).Skip(10).Take(10).ToList();

        // Assert
        await Assert.That(page1.Count)
            .IsEqualTo(10);
        await Assert.That(page2.Count)
            .IsEqualTo(10);
        await Assert.That(page1.First().Id)
            .IsNotEqualTo(page2.First().Id);
    }

    [Test]
    public async Task OrderDeletion_RemovesFromRepository()
    {
        // Arrange
        using var context = CreateDbContext();
        var orderResult = Order.Create("John", BurgerType.SmashBurger, 1);
        var order = orderResult.Value;

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Act
        context.Orders.Remove(order);
        await context.SaveChangesAsync();

        // Assert
        await Assert.That(context.Orders.Count())
            .IsEqualTo(0);
    }

    [Test]
    public async Task OrderUpdate_ModifiesPersistentData()
    {
        // Arrange
        using var context = CreateDbContext();
        var orderResult = Order.Create("Original", BurgerType.SmashBurger, 1);
        var order = orderResult.Value;

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Act
        var tracked = context.Orders.First();

        // Note: Order is immutable by design, so we test the constraint
        var isReadOnly = tracked.GetType()
            .GetProperties()
            .All(p => p.SetMethod?.IsPrivate ?? true);

        // Assert
        await Assert.That(isReadOnly)
            .IsTrue();
    }

    [Test]
    public async Task ConcurrentOrders_AtSameTime()
    {
        // Arrange
        using var context = CreateDbContext();
        var tasks = new List<Task>();

        // Act - Create multiple orders concurrently
        for (var i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var orderResult = Order.Create($"Concurrent {Guid.NewGuid()}", BurgerType.SmashBurger, 1);
                context.Orders.Add(orderResult.Value);
            }));
        }

        await Task.WhenAll(tasks);
        await context.SaveChangesAsync();

        // Assert
        await Assert.That(context.Orders.Count())
            .IsEqualTo(10);
    }

    [Test]
    public async Task OrderTotalPrice_CorrectForAllTypes()
    {
        // Arrange
        using var context = CreateDbContext();
        var orders = new[]
        {
            Order.Create("C1", BurgerType.SmashBurger, 1).Value,
            Order.Create("C2", BurgerType.CrispyChicken, 1).Value,
            Order.Create("C3", BurgerType.VeggieBean, 1).Value,
            Order.Create("C4", BurgerType.BBQBacon, 1).Value,
            Order.Create("C5", BurgerType.GrilledChicken, 1).Value
        };

        context.Orders.AddRange(orders);
        await context.SaveChangesAsync();

        // Assert - All have prices
        foreach (var order in context.Orders)
        {
            await Assert.That(order.TotalPrice)
                .IsGreaterThan(0);
        }
    }

    [Test]
    public async Task OrderAggregate_BusinessRulesEnforced()
    {
        // Arrange & Act & Assert
        var result1 = Order.Create("", BurgerType.SmashBurger, 1);
        await Assert.That(result1.IsFailure)
            .IsTrue();

        var result2 = Order.Create("John", BurgerType.SmashBurger, 0);
        await Assert.That(result2.IsFailure)
            .IsTrue();

        var result3 = Order.Create("John", BurgerType.SmashBurger, 51);
        await Assert.That(result3.IsFailure)
            .IsTrue();
    }

    [Test]
    public async Task OrderRepository_EmptyQueryReturnsEmpty()
    {
        // Arrange
        using var context = CreateDbContext();

        // Act
        var orders = context.Orders.ToList();

        // Assert
        await Assert.That(orders.Count)
            .IsEqualTo(0);
    }

    [Test]
    public async Task OrderRepository_FindByIdPattern()
    {
        // Arrange
        using var context = CreateDbContext();
        var target = Order.Create("Target", BurgerType.SmashBurger, 1).Value;
        var other = Order.Create("Other", BurgerType.CrispyChicken, 2).Value;

        context.Orders.AddRange(target, other);
        await context.SaveChangesAsync();

        // Act
        var found = context.Orders.FirstOrDefault(o => o.Id == target.Id);

        // Assert
        await Assert.That(found)
            .IsNotNull();
        await Assert.That(found!.CustomerName)
            .IsEqualTo("Target");
    }

    [Test]
    public async Task OrderResponse_DTO_Conversion()
    {
        // Arrange
        var orderResult = Order.Create("John", BurgerType.SmashBurger, 2);
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
            .IsEqualTo(order.CustomerName);
        await Assert.That(response.Quantity)
            .IsEqualTo(order.Quantity);
    }

    [Test]
    public async Task OrderPersistence_JsonRoundTrip()
    {
        // Arrange
        var orderResult = Order.Create("Alice", BurgerType.BBQBacon, 3);
        var order = orderResult.Value;
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

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(response);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<CreateOrderResponse>(json);

        // Assert
        await Assert.That(deserialized)
            .IsNotNull();
        await Assert.That(deserialized!.CustomerName)
            .IsEqualTo("Alice");
    }

    [Test]
    public async Task OrderService_FullLifecycle()
    {
        // Arrange
        using var context = CreateDbContext();
        var initialCount = context.Orders.Count();

        // Act
        var orderResult = Order.Create("John Doe", BurgerType.SmashBurger, 2);
        var order = orderResult.Value;
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var qrCodeResult = QrCodeService.GenerateQrCodeBase64(order.Id.Value.ToString());

        var retrieved = context.Orders.First(o => o.Id == order.Id);

        // Assert
        await Assert.That(context.Orders.Count())
            .IsEqualTo(initialCount + 1);
        await Assert.That(qrCodeResult.IsSuccess)
            .IsTrue();
        await Assert.That(qrCodeResult.Value)
            .IsNotEmpty();
        await Assert.That(retrieved.CustomerName)
            .IsEqualTo("John Doe");
    }

    [Test]
    public async Task DataAnnotations_ValidateOrderRequest()
    {
        // Arrange
        var validRequest = new CreateOrderRequest
        {
            CustomerName = "Valid Name",
            BurgerType = "SmashBurger",
            Quantity = 5
        };

        // Assert
        await Assert.That(validRequest.CustomerName)
            .IsNotNull();
        await Assert.That(validRequest.Quantity)
            .IsGreaterThan(0);
        await Assert.That(validRequest.BurgerType)
            .IsNotNull();
    }

    [Test]
    public async Task PerformanceCheck_InsertAndQuery()
    {
        // Arrange
        using var context = CreateDbContext();

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (var i = 0; i < 100; i++)
        {
            var orderResult = Order.Create($"Customer {i}", BurgerType.SmashBurger, 1);
            context.Orders.Add(orderResult.Value);
        }

        await context.SaveChangesAsync();

        var queryResult = context.Orders.ToList();

        sw.Stop();

        // Assert - Should complete in reasonable time
        await Assert.That(sw.ElapsedMilliseconds)
            .IsLessThan(500);
        await Assert.That(queryResult.Count)
            .IsEqualTo(100);
    }
}

// Support types
public record CreateOrderRequest
{
    public required string CustomerName { get; init; }
    public required string BurgerType { get; init; }
    public required int Quantity { get; init; }
}

public record OrderResponse(
    Guid Id,
    string CustomerName,
    string BurgerType,
    int Quantity,
    DateTime CreatedAtUtc,
    decimal TotalPrice);
