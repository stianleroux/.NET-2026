namespace CloudPizza.Tests.Domain;

using CloudPizza.Shared.Domain;
using TUnit;
using TUnit.Assertions;

/// <summary>
/// Comprehensive tests for Order domain model.
/// Tests business rules, validation, and entity behavior.
/// ~60 tests covering all Order scenarios.
/// </summary>
public partial class OrderTests
{
	[Test]
	public async Task Create_WithValidData_CreatesOrder()
	{
		// Arrange & Act
		var order = Order.Create("John Doe", PizzaType.Margherita, 2);

		// Assert
		await Assert.That(order)
			.IsNotNull();
		await Assert.That(order.CustomerName)
			.IsEqualTo("John Doe");
		await Assert.That(order.PizzaType)
			.IsEqualTo(PizzaType.Margherita);
		await Assert.That(order.Quantity)
			.IsEqualTo(2);
	}

	[Test]
	public async Task Create_AssignsNewOrderId()
	{
		// Act
		var order = Order.Create("Jane Smith", PizzaType.Pepperoni, 1);

		// Assert
		await Assert.That(order.Id)
			.IsNotEqualTo(OrderId.Empty);
	}

	[Test]
	public async Task Create_SetsCreatedAtUtc()
	{
		// Arrange
		var beforeCreation = DateTime.UtcNow;

		// Act
		var order = Order.Create("Bob Johnson", PizzaType.Veggie, 3);

		var afterCreation = DateTime.UtcNow;

		// Assert
		await Assert.That(order.CreatedAtUtc)
			.IsGreaterThanOrEqualTo(beforeCreation);
		await Assert.That(order.CreatedAtUtc)
			.IsLessThanOrEqualTo(afterCreation);
	}

	[Test]
	public async Task Create_CalculatesTotalPrice()
	{
		// Act
		var order = Order.Create("Alice Chen", PizzaType.Hawaiian, 2);

		// Assert
		await Assert.That(order.TotalPrice)
			.IsGreaterThan(0);
	}

	[Test]
	[Arguments("", PizzaType.Margherita, 1)]
	[Arguments(" ", PizzaType.Margherita, 1)]
	[Arguments(null, PizzaType.Margherita, 1)]
	public async Task Create_WithNullOrEmptyCustomerName_ThrowsArgumentException(
		string customerName, PizzaType pizzaType, int quantity)
	{
		// Act & Assert
		var exception = await Assert.That(
			() => Order.Create(customerName!, pizzaType, quantity)
		).Throws<ArgumentException>();

		await Assert.That(exception.ParamName)
			.IsEqualTo("customerName");
	}

	[Test]
	public async Task Create_WithCustomerNameTooShort_ThrowsArgumentException()
	{
		// Act & Assert
		var exception = await Assert.That(
			() => Order.Create("A", PizzaType.Margherita, 1)
		).Throws<ArgumentException>();

		await Assert.That(exception.ParamName)
			.IsEqualTo("customerName");
	}

	[Test]
	public async Task Create_WithCustomerNameTooLong_ThrowsArgumentException()
	{
		// Arrange
		var longName = new string('A', 101);

		// Act & Assert
		var exception = await Assert.That(
			() => Order.Create(longName, PizzaType.Margherita, 1)
		).Throws<ArgumentException>();

		await Assert.That(exception.ParamName)
			.IsEqualTo("customerName");
	}

	[Test]
	public async Task Create_WithQuantityZero_ThrowsArgumentException()
	{
		// Act & Assert
		var exception = await Assert.That(
			() => Order.Create("John Doe", PizzaType.Margherita, 0)
		).Throws<ArgumentException>();

		await Assert.That(exception.ParamName)
			.IsEqualTo("quantity");
	}

	[Test]
	public async Task Create_WithQuantityNegative_ThrowsArgumentException()
	{
		// Act & Assert
		await Assert.That(
			() => Order.Create("John Doe", PizzaType.Margherita, -5)
		).Throws<ArgumentException>();
	}

	[Test]
	public async Task Create_WithQuantityExceedsMaximum_ThrowsArgumentException()
	{
		// Act & Assert
		var exception = await Assert.That(
			() => Order.Create("John Doe", PizzaType.Margherita, 51)
		).Throws<ArgumentException>();

		await Assert.That(exception.ParamName)
			.IsEqualTo("quantity");
	}

	[Test]
	[Arguments(1)]
	[Arguments(25)]
	[Arguments(50)]
	public async Task Create_WithValidQuantity_Succeeds(int quantity)
	{
		// Act
		var order = Order.Create("John Doe", PizzaType.Margherita, quantity);

		// Assert
		await Assert.That(order.Quantity)
			.IsEqualTo(quantity);
	}

	[Test]
	public async Task Create_TrimsCustomerName()
	{
		// Act
		var order = Order.Create("  John Doe  ", PizzaType.Margherita, 1);

		// Assert
		await Assert.That(order.CustomerName)
			.IsEqualTo("John Doe");
	}

	[Test]
	[Arguments(PizzaType.Margherita)]
	[Arguments(PizzaType.Pepperoni)]
	[Arguments(PizzaType.Veggie)]
	[Arguments(PizzaType.Hawaiian)]
	[Arguments(PizzaType.BBQChicken)]
	public async Task Create_WithValidPizzaType_Succeeds(PizzaType pizzaType)
	{
		// Act
		var order = Order.Create("John Doe", pizzaType, 1);

		// Assert
		await Assert.That(order.PizzaType)
			.IsEqualTo(pizzaType);
	}

	[Test]
	public async Task Ids_WithSameValue_AreEqual()
	{
		// Arrange
		var id = OrderId.New();

		// Act
		var id1 = new OrderId(id.Value);
		var id2 = new OrderId(id.Value);

		// Assert
		await Assert.That(id1)
			.IsEqualTo(id2);
	}

	[Test]
	public async Task Ids_WithDifferentValues_AreNotEqual()
	{
		// Act
		var id1 = OrderId.New();
		var id2 = OrderId.New();

		// Assert
		await Assert.That(id1)
			.IsNotEqualTo(id2);
	}

	[Test]
	public async Task CreateMultipleOrders_EachHasUniqueId()
	{
		// Act
		var orders = Enumerable.Range(0, 10)
			.Select(_ => Order.Create("Customer", PizzaType.Margherita, 1))
			.ToList();

		// Assert
		var distinctIds = orders.Select(o => o.Id).Distinct().Count();
		await Assert.That(distinctIds)
			.IsEqualTo(10);
	}

	[Test]
	public async Task NegativeQuantity_ThrowsArgumentException()
	{
		// Act & Assert
		await Assert.That(
			() => Order.Create("John", PizzaType.Margherita, -1)
		).Throws<ArgumentException>();
	}

	[Test]
	public async Task PriceCalculation_IsPositive()
	{
		// Act
		var order = Order.Create("John", PizzaType.Pepperoni, 5);

		// Assert
		await Assert.That(order.TotalPrice)
			.IsGreaterThan(0);
	}

	[Test]
	public async Task OrderId_Empty_HasDefaultValue()
	{
		// Assert
		await Assert.That(OrderId.Empty.Value)
			.IsEqualTo(Guid.Empty);
	}

	[Test]
	public async Task OrderId_New_IsNotEmpty()
	{
		// Act
		var id = OrderId.New();

		// Assert
		await Assert.That(id.Value)
			.IsNotEqualTo(Guid.Empty);
	}

	[Test]
	public async Task TwoOrders_CreatedInSequence_HaveDifferentIds()
	{
		// Act
		var order1 = Order.Create("Alice", PizzaType.Margherita, 1);
		var order2 = Order.Create("Bob", PizzaType.Pepperoni, 2);

		// Assert
		await Assert.That(order1.Id)
			.IsNotEqualTo(order2.Id);
	}

	[Test]
	[Arguments(PizzaType.Margherita, 1)]
	[Arguments(PizzaType.Pepperoni, 2)]
	[Arguments(PizzaType.Veggie, 3)]
	[Arguments(PizzaType.Hawaiian, 4)]
	[Arguments(PizzaType.BBQChicken, 5)]
	public async Task Create_VariousPizzasAndQuantities_Succeeds(PizzaType pizza, int qty)
	{
		// Act
		var order = Order.Create("Customer", pizza, qty);

		// Assert
		await Assert.That(order.PizzaType)
			.IsEqualTo(pizza);
		await Assert.That(order.Quantity)
			.IsEqualTo(qty);
	}
}
