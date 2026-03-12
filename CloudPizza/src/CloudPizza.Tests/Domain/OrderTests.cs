namespace CloudBurger.Tests.Domain;

using CloudBurger.Shared.Domain;
using TUnit;
using TUnit.Assertions;
using TUnit.Assertions.AssertConditions.Throws;

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
		var orderResult = Order.Create("John Doe", BurgerType.SmashBurger, 2);
		var order = orderResult.Value;

		// Assert
		await Assert.That(order)
			.IsNotNull();
		await Assert.That(order.CustomerName)
			.IsEqualTo("John Doe");
		await Assert.That(order.BurgerType)
			.IsEqualTo(BurgerType.SmashBurger);
		await Assert.That(order.Quantity)
			.IsEqualTo(2);
	}

	[Test]
	public async Task Create_AssignsNewOrderId()
	{
		// Act
		var orderResult = Order.Create("Jane Smith", BurgerType.CrispyChicken, 1);
		var order = orderResult.Value;

		// Assert
		await Assert.That(order.Id.Value)
			.IsNotEqualTo(Guid.Empty);
	}

	[Test]
	public async Task Create_SetsCreatedAtUtc()
	{
		// Arrange
		var beforeCreation = DateTime.UtcNow;

		// Act
		var orderResult = Order.Create("Bob Johnson", BurgerType.VeggieBean, 3);
		var order = orderResult.Value;

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
		var orderResult = Order.Create("Alice Chen", BurgerType.BBQBacon, 2);
		var order = orderResult.Value;

		// Assert
		await Assert.That(order.TotalPrice)
			.IsGreaterThan(0);
	}

	[Test]
	[Arguments("", BurgerType.SmashBurger, 1)]
	[Arguments(" ", BurgerType.SmashBurger, 1)]
	[Arguments(null, BurgerType.SmashBurger, 1)]
	public async Task Create_WithNullOrEmptyCustomerName_ReturnsFailure(
		string customerName, BurgerType burgerType, int quantity)
	{
		// Act
		var result = Order.Create(customerName!, burgerType, quantity);

		// Assert
		await Assert.That(result.IsFailure)
			.IsTrue();
		await Assert.That(result.ValidationErrors)
			.IsNotNull();
	}

	[Test]
	public async Task Create_WithCustomerNameTooShort_ReturnsFailure()
	{
		// Act
		var result = Order.Create("A", BurgerType.SmashBurger, 1);

		// Assert
		await Assert.That(result.IsFailure)
			.IsTrue();
		await Assert.That(result.ValidationErrors)
			.IsNotNull();
	}

	[Test]
	public async Task Create_WithCustomerNameTooLong_ReturnsFailure()
	{
		// Arrange
		var longName = new string('A', 101);

		// Act
		var result = Order.Create(longName, BurgerType.SmashBurger, 1);

		// Assert
		await Assert.That(result.IsFailure)
			.IsTrue();
		await Assert.That(result.ValidationErrors)
			.IsNotNull();
	}

	[Test]
	public async Task Create_WithQuantityZero_ReturnsFailure()
	{
		// Act
		var result = Order.Create("John Doe", BurgerType.SmashBurger, 0);

		// Assert
		await Assert.That(result.IsFailure)
			.IsTrue();
		await Assert.That(result.ValidationErrors)
			.IsNotNull();
	}

	[Test]
	public async Task Create_WithQuantityNegative_ReturnsFailure()
	{
		// Act
		var result = Order.Create("John Doe", BurgerType.SmashBurger, -5);

		// Assert
		await Assert.That(result.IsFailure)
			.IsTrue();
	}

	[Test]
	public async Task Create_WithQuantityExceedsMaximum_ReturnsFailure()
	{
		// Act
		var result = Order.Create("John Doe", BurgerType.SmashBurger, 51);

		// Assert
		await Assert.That(result.IsFailure)
			.IsTrue();
		await Assert.That(result.ValidationErrors)
			.IsNotNull();
	}

	[Test]
	[Arguments(1)]
	[Arguments(25)]
	[Arguments(50)]
	public async Task Create_WithValidQuantity_Succeeds(int quantity)
	{
		// Act
		var orderResult = Order.Create("John Doe", BurgerType.SmashBurger, quantity);
		var order = orderResult.Value;

		// Assert
		await Assert.That(order.Quantity)
			.IsEqualTo(quantity);
	}

	[Test]
	public async Task Create_TrimsCustomerName()
	{
		// Act
		var orderResult = Order.Create("  John Doe  ", BurgerType.SmashBurger, 1);
		var order = orderResult.Value;

        // Assert
        await Assert.That(order.CustomerName)
			.IsEqualTo("John Doe");
	}

	[Test]
	[Arguments(BurgerType.SmashBurger)]
	[Arguments(BurgerType.CrispyChicken)]
	[Arguments(BurgerType.VeggieBean)]
	[Arguments(BurgerType.BBQBacon)]
	[Arguments(BurgerType.GrilledChicken)]
	public async Task Create_WithValidBurgerType_Succeeds(BurgerType burgerType)
	{
		// Act
		var orderResult = Order.Create("John Doe", burgerType, 1);
		var order = orderResult.Value;

		// Assert
		await Assert.That(order.BurgerType)
			.IsEqualTo(burgerType);
	}

	[Test]
	public async Task Ids_WithSameValue_AreEqual()
	{
		// Arrange
		var id = OrderId.New();

		// Act
		var id1Result = OrderId.Create(id.Value);
		var id2Result = OrderId.Create(id.Value);

		// Assert
		await Assert.That(id1Result.IsSuccess).IsTrue();
		await Assert.That(id2Result.IsSuccess).IsTrue();
		await Assert.That(id1Result.Value)
			.IsEqualTo(id2Result.Value);
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
			.Select(_ => Order.Create("Customer", BurgerType.SmashBurger, 1).Value)
			.ToList();

		// Assert
		var distinctIds = orders.Select(o => o.Id).Distinct().Count();
		await Assert.That(distinctIds)
			.IsEqualTo(10);
	}

	[Test]
	public async Task NegativeQuantity_ReturnsFailure()
	{
		// Act
		var result = Order.Create("John", BurgerType.SmashBurger, -1);

		// Assert
		await Assert.That(result.IsFailure)
			.IsTrue();
	}

	[Test]
	public async Task PriceCalculation_IsPositive()
	{
		// Act
		var orderResult = Order.Create("John", BurgerType.CrispyChicken, 5);
		var order = orderResult.Value;

		// Assert
		await Assert.That(order.TotalPrice)
			.IsGreaterThan(0);
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
		var order1 = Order.Create("Alice", BurgerType.SmashBurger, 1).Value;
		var order2 = Order.Create("Bob", BurgerType.CrispyChicken, 2).Value;

		// Assert
		await Assert.That(order1.Id)
			.IsNotEqualTo(order2.Id);
	}

	[Test]
	[Arguments(BurgerType.SmashBurger, 1)]
	[Arguments(BurgerType.CrispyChicken, 2)]
	[Arguments(BurgerType.VeggieBean, 3)]
	[Arguments(BurgerType.BBQBacon, 4)]
	[Arguments(BurgerType.GrilledChicken, 5)]
	public async Task Create_VariousBurgersAndQuantities_Succeeds(BurgerType burger, int qty)
	{
		// Act
		var orderResult = Order.Create("Customer", burger, qty);
		var order = orderResult.Value;

		// Assert
		await Assert.That(order.BurgerType)
			.IsEqualTo(burger);
		await Assert.That(order.Quantity)
			.IsEqualTo(qty);
	}
}
