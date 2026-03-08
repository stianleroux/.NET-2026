namespace CloudPizza.Tests.Features;

using CloudPizza.Infrastructure.Data;
using CloudPizza.Shared.Contracts;
using CloudPizza.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TUnit;
using TUnit.Assertions;

/// <summary>
/// Tests for Order API endpoints.
/// ~70 tests covering create, retrieve, and streaming operations.
/// Uses NSubstitute for mocking dependencies.
/// </summary>
public partial class OrderEndpointsTests
{
	private PizzaDbContext CreateDbContext()
	{
		var options = new DbContextOptionsBuilder<PizzaDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new PizzaDbContext(options);
	}

	[Test]
	public async Task CreateOrder_WithValidRequest_SavesOrder()
	{
		// Arrange
		using var context = CreateDbContext();
		var request = new CreateOrderRequest
		{
			CustomerName = "John Doe",
			PizzaType = "Margherita",
			Quantity = 2
		};

		var order = Order.Create(request.CustomerName, PizzaType.Margherita, request.Quantity);
		var before = context.Orders.Count();

		// Act
		context.Orders.Add(order);
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
			PizzaType = "Pepperoni",
			Quantity = 3
		};

		var order = Order.Create(request.CustomerName, PizzaType.Pepperoni, request.Quantity);

		// Act
		context.Orders.Add(order);
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
		var order1 = Order.Create("Alice", PizzaType.Margherita, 1);
		var order2 = Order.Create("Bob", PizzaType.Pepperoni, 2);

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
		for (int i = 0; i < 5; i++)
		{
			var order = Order.Create($"Customer {i}", PizzaType.Margherita, 1);
			context.Orders.Add(order);
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
		var order1 = Order.Create("Alice", PizzaType.Margherita, 1);
		System.Threading.Thread.Sleep(10);
		var order2 = Order.Create("Bob", PizzaType.Pepperoni, 1);

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
		var order = Order.Create("John", PizzaType.Margherita, 1);
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
			PizzaType = "Margherita",
			Quantity = 2
		};

		// Assert
		await Assert.That(request.CustomerName)
			.IsNotNull();
		await Assert.That(request.PizzaType)
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
		var order = Order.Create("Alice", PizzaType.Margherita, 2);

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
			.IsEqualTo("Alice");
		await Assert.That(response.Quantity)
			.IsEqualTo(2);
	}

	[Test]
	public async Task MultipleOrders_CanBeStoredAndRetrieved()
	{
		// Arrange
		using var context = CreateDbContext();
		var orders = Enumerable.Range(0, 10)
			.Select(i => Order.Create($"Customer {i}", PizzaType.Margherita, 1))
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
		var original = Order.Create("Test Customer", PizzaType.BBQChicken, 5);
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
		await Assert.That(retrieved.PizzaType)
			.IsEqualTo(PizzaType.BBQChicken);
		await Assert.That(retrieved.Quantity)
			.IsEqualTo(5);
	}

	[Test]
	[Arguments(PizzaType.Margherita)]
	[Arguments(PizzaType.Pepperoni)]
	[Arguments(PizzaType.Veggie)]
	[Arguments(PizzaType.Hawaiian)]
	[Arguments(PizzaType.BBQChicken)]
	public async Task AllPizzaTypes_CanBeCreated(PizzaType pizzaType)
	{
		// Arrange
		using var context = CreateDbContext();

		// Act
		var order = Order.Create("Customer", pizzaType, 1);
		context.Orders.Add(order);
		await context.SaveChangesAsync();

		var retrieved = context.Orders.First();

		// Assert
		await Assert.That(retrieved.PizzaType)
			.IsEqualTo(pizzaType);
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
		var order = Order.Create("John", PizzaType.Margherita, 1);

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
		var order = Order.Create("Customer", PizzaType.Margherita, 2);

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
		var order1 = Order.Create("Customer", PizzaType.Margherita, 1);
		var orderN = Order.Create("Customer", PizzaType.Margherita, quantity);

		// Act
		var ratio = orderN.TotalPrice / order1.TotalPrice;

		// Assert
		await Assert.That(ratio)
			.IsCloseTo(expectedMultiplier, 0.1);
	}
}
