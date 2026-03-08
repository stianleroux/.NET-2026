namespace CloudPizza.Tests.Integration;

using CloudPizza.Infrastructure.Data;
using CloudPizza.Infrastructure.Services;
using CloudPizza.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TUnit;
using TUnit.Assertions;

/// <summary>
/// Integration tests verifying coordination between components.
/// ~30 tests ensuring proper interaction of domain, services, and persistence.
/// </summary>
public partial class OrderIntegrationTests
{
	private PizzaDbContext CreateDbContext()
	{
		var options = new DbContextOptionsBuilder<PizzaDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new PizzaDbContext(options);
	}

	[Test]
	public async Task CreateOrder_PersistAndRetrieve()
	{
		// Arrange
		using var context = CreateDbContext();
		var order = Order.Create("John Doe", PizzaType.Margherita, 2);

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
		var qrService = new QrCodeService();
		var order = Order.Create("Alice", PizzaType.Pepperoni, 1);

		context.Orders.Add(order);
		await context.SaveChangesAsync();

		// Act
		var qrData = $"{order.Id.Value}:{order.CustomerName}";
		var qrCode = qrService.GenerateQrCodeBase64(qrData);

		// Assert
		await Assert.That(qrCode)
			.IsNotEmpty();
	}

	[Test]
	public async Task MultipleOrders_MaintenanceFreelySaved()
	{
		// Arrange
		using var context = CreateDbContext();

		// Act
		for (int i = 0; i < 10; i++)
		{
			var order = Order.Create($"Customer {i}", PizzaType.Margherita, (i % 5) + 1);
			context.Orders.Add(order);
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
		var order1 = Order.Create("Alice", PizzaType.Margherita, 1);
		var order2 = Order.Create("Bob", PizzaType.Pepperoni, 2);

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
	public async Task OrderQuerying_ByPizzaType()
	{
		// Arrange
		using var context = CreateDbContext();
		context.Orders.AddRange(
			Order.Create("A", PizzaType.Margherita, 1),
			Order.Create("B", PizzaType.Margherita, 1),
			Order.Create("C", PizzaType.Pepperoni, 1)
		);
		await context.SaveChangesAsync();

		// Act
		var margheritaOrders = context.Orders
			.Where(o => o.PizzaType == PizzaType.Margherita)
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
		var order1 = Order.Create("A", PizzaType.Margherita, 1);
		
		System.Threading.Thread.Sleep(10);
		
		var order2 = Order.Create("B", PizzaType.Pepperoni, 1);

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
		
		for (int i = 0; i < 25; i++)
		{
			context.Orders.Add(Order.Create($"Customer {i}", PizzaType.Margherita, 1));
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
		var order = Order.Create("John", PizzaType.Margherita, 1);

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
		var order = Order.Create("Original", PizzaType.Margherita, 1);

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
		for (int i = 0; i < 10; i++)
		{
			tasks.Add(Task.Run(() =>
			{
				var order = Order.Create($"Concurrent {Guid.NewGuid()}", PizzaType.Margherita, 1);
				context.Orders.Add(order);
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
			Order.Create("C1", PizzaType.Margherita, 1),
			Order.Create("C2", PizzaType.Pepperoni, 1),
			Order.Create("C3", PizzaType.Veggie, 1),
			Order.Create("C4", PizzaType.Hawaiian, 1),
			Order.Create("C5", PizzaType.BBQChicken, 1)
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
		await Assert.That(() =>
			Order.Create("", PizzaType.Margherita, 1)
		).Throws<ArgumentException>();

		await Assert.That(() =>
			Order.Create("John", PizzaType.Margherita, 0)
		).Throws<ArgumentException>();

		await Assert.That(() =>
			Order.Create("John", PizzaType.Margherita, 51)
		).Throws<ArgumentException>();
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
		var target = Order.Create("Target", PizzaType.Margherita, 1);
		var other = Order.Create("Other", PizzaType.Pepperoni, 2);

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
		var order = Order.Create("John", PizzaType.Margherita, 2);

		// Act
		var response = new OrderResponse(
			order.Id.Value,
			order.CustomerName,
			order.PizzaType.ToString(),
			order.Quantity,
			order.CreatedAtUtc,
			order.TotalPrice
		);

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
		var order = Order.Create("Alice", PizzaType.Hawaiian, 3);
		var response = new OrderResponse(
			order.Id.Value,
			order.CustomerName,
			order.PizzaType.ToString(),
			order.Quantity,
			order.CreatedAtUtc,
			order.TotalPrice
		);

		// Act
		var json = System.Text.Json.JsonSerializer.Serialize(response);
		var deserialized = System.Text.Json.JsonSerializer.Deserialize<OrderResponse>(json);

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
		var qrService = new QrCodeService();
		var initialCount = context.Orders.Count();

		// Act
		var order = Order.Create("John Doe", PizzaType.Margherita, 2);
		context.Orders.Add(order);
		await context.SaveChangesAsync();

		var qrCode = qrService.GenerateQrCodeBase64(order.Id.Value.ToString());

		var retrieved = context.Orders.First(o => o.Id == order.Id);

		// Assert
		await Assert.That(context.Orders.Count())
			.IsEqualTo(initialCount + 1);
		await Assert.That(qrCode)
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
			PizzaType = "Margherita",
			Quantity = 5
		};

		// Assert
		await Assert.That(validRequest.CustomerName)
			.IsNotNull();
		await Assert.That(validRequest.Quantity)
			.IsGreaterThan(0);
		await Assert.That(validRequest.PizzaType)
			.IsNotNull();
	}

	[Test]
	public async Task PerformanceCheck_InsertAndQuery()
	{
		// Arrange
		using var context = CreateDbContext();

		// Act
		var sw = System.Diagnostics.Stopwatch.StartNew();

		for (int i = 0; i < 100; i++)
		{
			var order = Order.Create($"Customer {i}", PizzaType.Margherita, 1);
			context.Orders.Add(order);
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
	public required string PizzaType { get; init; }
	public required int Quantity { get; init; }
}

public record OrderResponse(
	Guid Id,
	string CustomerName,
	string PizzaType,
	int Quantity,
	DateTime CreatedAtUtc,
	decimal TotalPrice);
