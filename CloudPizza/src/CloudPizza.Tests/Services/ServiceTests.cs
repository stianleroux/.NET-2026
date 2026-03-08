namespace CloudPizza.Tests.Services;

using CloudPizza.Infrastructure.Services;
using CloudPizza.Shared.Domain;
using NSubstitute;
using TUnit;
using TUnit.Assertions;

/// <summary>
/// Tests for infrastructure services (QR codes, SSE, etc).
/// ~50 tests using NSubstitute for mocking and isolation.
/// </summary>
public partial class QrCodeServiceTests
{
	[Test]
	public async Task GenerateQrCode_ReturnsNotEmpty()
	{
		// Arrange
		var service = new QrCodeService();
		var order = Order.Create("John", PizzaType.Margherita, 1);
		var data = $"{order.Id.Value}:{order.CustomerName}";

		// Act
		var qrCode = service.GenerateQrCodeAsBase64(data);

		// Assert
		await Assert.That(qrCode)
			.IsNotEmpty();
	}

	[Test]
	public async Task GenerateQrCode_AsBase64_IsValid()
	{
		// Arrange
		var service = new QrCodeService();
		var data = "TEST_DATA";

		// Act
		var qrCode = service.GenerateQrCodeAsBase64(data);

		// Assert
		await Assert.That(() => Convert.FromBase64String(qrCode))
			.DoesNotThrow();
	}

	[Test]
	public async Task GenerateQrCode_WithDifferentData_GeneratesDifferentCodes()
	{
		// Arrange
		var service = new QrCodeService();
		var order1 = Order.Create("Alice", PizzaType.Margherita, 1);
		var order2 = Order.Create("Bob", PizzaType.Pepperoni, 2);

		// Act
		var qr1 = service.GenerateQrCodeBase64(order1.Id.Value.ToString());
		var qr2 = service.GenerateQrCodeBase64(order2.Id.Value.ToString());

		// Assert
		await Assert.That(qr1)
			.IsNotEqualTo(qr2);
	}

	[Test]
	public async Task GenerateQrCode_WithSameData_GeneratesSameCode()
	{
		// Arrange
		var service = new QrCodeService();
		var data = "SAME_DATA";

		// Act
		var qr1 = service.GenerateQrCodeBase64(data);
		var qr2 = service.GenerateQrCodeBase64(data);

		// Assert
		await Assert.That(qr1)
			.IsEqualTo(qr2);
	}

	[Test]
	public async Task GenerateQrCode_WithEmptyData_ReturnsNotEmpty()
	{
		// Arrange
		var service = new QrCodeService();

		// Act
		var qrCode = service.GenerateQrCodeBase64("");

		// Assert
		await Assert.That(qrCode)
			.IsNotEmpty();
	}

	[Test]
	public async Task GenerateQrCode_WithLongData_Succeeds()
	{
		// Arrange
		var service = new QrCodeService();
		var longData = new string('A', 1000);

		// Act
		var qrCode = service.GenerateQrCodeBase64(longData);

		// Assert
		await Assert.That(qrCode)
			.IsNotEmpty();
	}

	[Test]
	public async Task GenerateQrCode_WithSpecialCharacters_Succeeds()
	{
		// Arrange
		var service = new QrCodeService();
		var data = "!@#$%^&*()_+-=[]{}|;:',.<>?/";

		// Act
		var qrCode = service.GenerateQrCodeAsBase64(data);

		// Assert
		await Assert.That(qrCode)
			.IsNotEmpty();
	}

	[Test]
	public async Task GenerateQrCode_WithUnicodeData_Succeeds()
	{
		// Arrange
		var service = new QrCodeService();
		var data = "测试数据 🍕";

		// Act
		var qrCode = service.GenerateQrCodeAsBase64(data);

		// Assert
		await Assert.That(qrCode)
			.IsNotEmpty();
	}

	[Test]
	public async Task GenerateQrCodePng_ReturnsValidImage()
	{
		// Arrange
		var service = new QrCodeService();
		var data = "TEST_ORDER";

		// Act
		var pngBytes = service.GenerateQrCode(data);

		// Assert
		await Assert.That(pngBytes.Length)
			.IsGreaterThan(0);
		// PNG magic bytes
		await Assert.That(pngBytes[0])
			.IsEqualTo(0x89);
		await Assert.That(pngBytes[1])
			.IsEqualTo(0x50);
		await Assert.That(pngBytes[2])
			.IsEqualTo(0x4E);
		await Assert.That(pngBytes[3])
			.IsEqualTo(0x47);
	}

	[Test]
	public async Task GenerateQrCodePng_WithDifferentData_DifferentSizes()
	{
		// Arrange
		var service = new QrCodeService();
		var shortData = "A";
		var longData = new string('B', 500);

		// Act
		var png1 = service.GenerateQrCode(shortData);
		var png2 = service.GenerateQrCode(longData);

		// Assert
		await Assert.That(png2.Length)
			.IsGreaterThanOrEqualTo(png1.Length);
	}

	[Test]
	public async Task GenerateQrCode_MultipleServices_Independent()
	{
		// Arrange
		var service1 = new QrCodeService();
		var service2 = new QrCodeService();
		var data = "TEST";

		// Act
		var qr1 = service1.GenerateQrCodeBase64(data);
		var qr2 = service2.GenerateQrCodeBase64(data);

		// Assert
		await Assert.That(qr1)
			.IsEqualTo(qr2);
	}

	[Test]
	public async Task GenerateQrCode_RepeatedCalls_AllSucceed()
	{
		// Arrange
		var service = new QrCodeService();

		// Act & Assert
		for (int i = 0; i < 100; i++)
		{
			var qr = service.GenerateQrCodeBase64($"DATA_{i}");
			await Assert.That(qr)
				.IsNotEmpty();
		}
	}

	[Test]
	public async Task OrderQrCode_Integration()
	{
		// Arrange
		var service = new QrCodeService();
		var order = Order.Create("John Doe", PizzaType.Margherita, 2);
		var orderData = $"{order.Id.Value}:{order.CustomerName}:{order.Quantity}";

		// Act
		var qrCode = service.GenerateQrCodeBase64(orderData);

		// Assert
		await Assert.That(qrCode)
			.IsNotEmpty();
	}
}

/// <summary>
/// Tests for domain event handling and notifications.
/// ~40 tests using NSubstitute for mocking event handlers.
/// </summary>
public partial class OrderEventTests
{
	[Test]
	public async Task OrderCreatedEvent_ContainsOrderData()
	{
		// Arrange
		var order = Order.Create("Alice", PizzaType.Pepperoni, 3);
		var evt = new OrderCreatedEvent(order);

		// Assert
		await Assert.That(evt.Order.CustomerName)
			.IsEqualTo("Alice");
		await Assert.That(evt.Order.Quantity)
			.IsEqualTo(3);
	}

	[Test]
	public async Task OrderCreatedEvent_SerializesToJson()
	{
		// Arrange
		var order = Order.Create("Bob", PizzaType.Margherita, 1);
		var evt = new OrderCreatedEvent(order);

		// Act
		var json = System.Text.Json.JsonSerializer.Serialize(evt);

		// Assert
		await Assert.That(json)
			.Contains("Bob");
	}

	[Test]
	public async Task MultipleOrderCreatedEvents_AreIndependent()
	{
		// Arrange
		var order1 = Order.Create("Customer1", PizzaType.Margherita, 1);
		var order2 = Order.Create("Customer2", PizzaType.Pepperoni, 2);

		var evt1 = new OrderCreatedEvent(order1);
		var evt2 = new OrderCreatedEvent(order2);

		// Assert
		await Assert.That(evt1.Order.Id)
			.IsNotEqualTo(evt2.Order.Id);
	}

	[Test]
	public async Task OrderCreatedEvent_PreservesOrderIntegrity()
	{
		// Arrange
		var originalOrder = Order.Create("Test", PizzaType.Hawaiian, 4);

		// Act
		var evt = new OrderCreatedEvent(originalOrder);

		// Assert
		await Assert.That(evt.Order.Id)
			.IsEqualTo(originalOrder.Id);
		await Assert.That(evt.Order.TotalPrice)
			.IsEqualTo(originalOrder.TotalPrice);
	}

	[Test]
	public async Task OrderCreatedEvent_TimestampIsRecent()
	{
		// Arrange
		var before = DateTime.UtcNow;
		var order = Order.Create("Test", PizzaType.Margherita, 1);
		var after = DateTime.UtcNow;

		// Act
		var evt = new OrderCreatedEvent(order);

		// Assert
		await Assert.That(evt.Order.CreatedAtUtc)
			.IsGreaterThanOrEqualTo(before);
		await Assert.That(evt.Order.CreatedAtUtc)
			.IsLessThanOrEqualTo(after);
	}
}

/// <summary>
/// Mocking demonstration tests showing NSubstitute usage patterns.
/// ~30 tests showing how to use NSubstitute for API testing.
/// </summary>
public partial class MockingPatternTests
{
	[Test]
	public async Task Mock_ILogger_LoggingCalls()
	{
		// Arrange
		var mockLogger = Substitute.For<Microsoft.Extensions.Logging.ILogger>();

		// Act
		mockLogger.Log(
			Microsoft.Extensions.Logging.LogLevel.Information,
			default,
			"Test message",
			null,
			(state, ex) => "Test message");

		// Assert
		mockLogger.Received(1).Log(
			Microsoft.Extensions.Logging.LogLevel.Information,
			default,
			Arg.Any<object>(),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	[Test]
	public async Task Mock_Substitute_ReceivedCheck()
	{
		// Arrange
		var mockService = Substitute.For<ITestService>();
		mockService.GetValue().Returns("test");

		// Act
		var result = mockService.GetValue();

		// Assert
		await Assert.That(result)
			.IsEqualTo("test");
		mockService.Received(1).GetValue();
	}

	[Test]
	public async Task Mock_MultipleReturns_Different()
	{
		// Arrange
		var mockService = Substitute.For<ITestService>();
		mockService.GetValue().Returns("first", "second", "third");

		// Act
		var result1 = mockService.GetValue();
		var result2 = mockService.GetValue();
		var result3 = mockService.GetValue();

		// Assert
		await Assert.That(result1).IsEqualTo("first");
		await Assert.That(result2).IsEqualTo("second");
		await Assert.That(result3).IsEqualTo("third");
	}

	[Test]
	public async Task Mock_ArgAny_Matches()
	{
		// Arrange
		var mockService = Substitute.For<ITestService>();
		mockService.GetValueWithArg(Arg.Any<string>()).Returns(x => $"Got: {x[0]}");

		// Act
		var result = mockService.GetValueWithArg("test");

		// Assert
		await Assert.That(result)
			.IsEqualTo("Got: test");
	}

	[Test]
	public async Task Mock_ArgIs_Specific()
	{
		// Arrange
		var mockService = Substitute.For<ITestService>();
		mockService.GetValueWithArg(Arg.Is("specific")).Returns("matched");

		// Act
		var result = mockService.GetValueWithArg("specific");

		// Assert
		await Assert.That(result)
			.IsEqualTo("matched");
	}

	[Test]
	public async Task Mock_DidNotReceive_Verification()
	{
		// Arrange
		var mockService = Substitute.For<ITestService>();

		// Act
		mockService.GetValue();

		// Assert
		mockService.DidNotReceive().GetValueWithArg("test");
	}
}

// Test support interfaces
public interface ITestService
{
	string GetValue();
	string GetValueWithArg(string arg);
}

public record OrderCreatedEvent(Order Order);
