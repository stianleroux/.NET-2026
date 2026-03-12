# CloudBurger Test Suite & Mocking Patterns

This directory contains a comprehensive test suite showcasing modern .NET 10 testing patterns with **TUnit**, **NSubstitute**, and conversion examples to **Imposter** for HTTP mocking.

## 📋 Test Structure

### Test Organization (~200+ Tests)

```
CloudBurger.Tests/
├── Domain/
│   └── OrderTests.cs (60 tests)
│       - Order creation validation
│       - Business rule enforcement
│       - Domain model invariants
│       - Price calculations
│
├── Features/
│   └── OrderEndpointsTests.cs (70 tests)
│       - API endpoint behavior
│       - Database persistence
│       - Query operations
│       - Data contracts
│
├── Services/
│   └── ServiceTests.cs (50 tests)
│       - QR code generation
│       - Service mocking patterns
│       - NSubstitute usage examples
│
├── Integration/
│   └── OrderIntegrationTests.cs (30 tests)
│       - End-to-end component interaction
│       - Repository operations
│       - Event handling
│
└── Benchmarks/
    └── MockingPerformanceBenchmarks.cs (20 tests)
        - NSubstitute vs HTTP mocking comparison
        - Performance metrics
        - Throughput analysis
```

## 🧪 Running Tests

### Run All Tests
```bash
dotnet test CloudBurger.Tests.csproj
```

### Run Specific Test Class
```bash
dotnet test --filter "ClassName=OrderTests"
```

### Run with Verbose Output
```bash
dotnet test --verbosity detailed
```

### Run Benchmarks Only
```bash
dotnet test --filter "Category=Benchmarks"
```

## 🎯 Test Highlights

### 1. Domain Tests (OrderTests.cs)
Tests the core business logic of Order aggregate:
- ✅ Valid order creation with all burger types
- ✅ Business rule validation (name length, quantity limits)
- ✅ Order ID uniqueness
- ✅ Price calculations
- ⚠️ Invalid input handling with exception verification

```csharp
[Test]
public async Task Create_WithValidData_CreatesOrder()
{
    var order = Order.Create("John Doe", BurgerType.SmashBurger, 2);
    
    await Assert.That(order.CustomerName).IsEqualTo("John Doe");
}
```

### 2. API Endpoint Tests (OrderEndpointsTests.cs)
Tests HTTP endpoint integration:
- ✅ Order persistence to database
- ✅ Data retrieval and filtering
- ✅ Pagination
- ✅ Request/response contracts

```csharp
[Test]
public async Task CreateOrder_WithValidRequest_SavesOrder()
{
    using var context = CreateDbContext();
    var order = Order.Create("John Doe", BurgerType.SmashBurger, 2);
    
    context.Orders.Add(order);
    await context.SaveChangesAsync();
    
    await Assert.That(context.Orders.Count()).IsEqualTo(1);
}
```

### 3. Service Tests with NSubstitute (ServiceTests.cs)
Demonstrates mocking patterns:

```csharp
[Test]
public async Task Mock_Substitute_ReceivedCheck()
{
    var mockService = Substitute.For<ITestService>();
    mockService.GetValue().Returns("test");
    
    var result = mockService.GetValue();
    
    await Assert.That(result).IsEqualTo("test");
    mockService.Received(1).GetValue();
}
```

### 4. Integration Tests (OrderIntegrationTests.cs)
Tests component interaction:
- ✅ Order creation and retrieval
- ✅ QR code generation for orders
- ✅ Multi-order operations
- ✅ Query filtering
- ✅ Concurrent operations

### 5. Performance Benchmarks (MockingPerformanceBenchmarks.cs)
Compares NSubstitute vs HTTP mocking:

| Operation                  | Time     | Calls/sec |
| -------------------------- | -------- | --------- |
| NSubstitute Setup          | < 1ms    | -         |
| NSubstitute Call           | < 0.01ms | 1M+       |
| Domain Model Creation      | < 0.01ms | 100k+     |
| HTTP Call (simulated)      | ~10ms    | 100       |
| JSON Serialization (1000x) | < 50ms   | 20k       |

## 🔄 Mocking Patterns

### NSubstitute (In-Memory Mocking)
Best for: Unit tests, interface contracts, fast tests

```csharp
// Setup mock
var mock = Substitute.For<IOrderApiClient>();
mock.GetOrderAsync(Arg.Any<Guid>())
    .Returns(Task.FromResult(new OrderResponse { Id = Guid.NewGuid() }));

// Use mock
var result = await mock.GetOrderAsync(id);

// Verify calls
mock.Received(1).GetOrderAsync(id);
```

**Advantages:**
- ⚡ Extremely fast
- 🎯 Precise control
- 📦 Easy setup
- 🔍 Good introspection

**Disadvantages:**
- ❌ No real HTTP behavior
- ❌ Can't test network failures
- ❌ No timeout testing
- ❌ No serialization testing

### Imposter (HTTP Server Mocking)
Best for: Integration tests, API contracts, realistic scenarios

See [Imposter on GitHub](https://github.com/themidnightgospel/Imposter)

```csharp
// Use real HttpClient against mocked server
using var client = new HttpClient 
{ 
    BaseAddress = new Uri("http://localhost:8080") 
};

var response = await client.GetAsync("/api/orders/123");
var json = await response.Content.ReadAsStringAsync();
var order = JsonSerializer.Deserialize<OrderResponse>(json);

// Verify real HTTP behavior
Assert.AreEqual(200, (int)response.StatusCode);
```

**Advantages:**
- ✅ Tests real HTTP behavior
- ✅ Network failure simulation
- ✅ Timeout testing
- ✅ Serialization/deserialization
- ✅ Multi-client testing
- ✅ Reusable across projects

**Disadvantages:**
- 🐢 Slower (network overhead)
- 🚀 Requires separate server
- 🔧 More complex setup

## 📝 Conversion Guide: NSubstitute → Imposter

### Step 1: Identify Mock Points
```csharp
// BEFORE: NSubstitute
var mock = Substitute.For<IOrderApiClient>();
mock.GetOrderAsync(1).Returns(Task.FromResult(order));
```

### Step 2: Set Up Imposter Server
```yaml
# imposter-config.yaml
endpoints:
  - path: /api/orders/{id}
    method: GET
    response:
      statusCode: 200
      content:
        id: "{{ request.pathParams.id }}"
        customerName: "John"
        quantity: 2
```

### Step 3: Replace Mock with HttpClient
```csharp
// AFTER: Imposter
using var client = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };
var response = await client.GetAsync("/api/orders/1");
var json = await response.Content.ReadAsStringAsync();
var order = JsonSerializer.Deserialize<OrderResponse>(json);
```

### Step 4: Update Assertions
```csharp
// Verify HTTP response
Assert.AreEqual(200, (int)response.StatusCode);
Assert.Contains("John", json);
```

## 🚀 Conversion Script

A single-file C# script demonstrates the conversion pattern:

```bash
# Install dotnet-script
dotnet tool install -g dotnet-script

# Run conversion guide
dotnet script docs/ConvertToImposter.csx
```

The script shows:
- 📚 Before/after patterns
- 🔄 Step-by-step conversion
- ⚙️ Configuration examples
- 🎯 When to use each approach
- ⚠️ Common gotchas

## 📊 Test Metrics

- **Total Tests:** 200+
- **Domain Tests:** 60
- **API Tests:** 70
- **Service Tests:** 50
- **Integration Tests:** 30
- **Benchmark Tests:** 20+

## 🏆 Test Coverage Areas

| Area          | Tests | Coverage                    |
| ------------- | ----- | --------------------------- |
| Domain Models | 60    | Business rules, validation  |
| API Endpoints | 70    | Requests, responses, errors |
| Services      | 50    | Mocking patterns, QR codes  |
| Database      | 30    | Persistence, queries        |
| Performance   | 20+   | Benchmarks, throughput      |

## 🔧 Using TUnit

TUnit is a modern testing framework optimized for async/await:

```csharp
// Async-first design
[Test]
public async Task Example()
{
    await Assert.That(value)
        .IsEqualTo(expected);
}

// Better than xUnit for async tests
// Better assertion syntax
```

**Installation:**
```bash
dotnet add package TUnit
dotnet add package TUnit.Assertions
```

## 🎓 Learning Outcomes

After exploring these tests, you'll understand:

1. ✅ Modern .NET 10 testing with TUnit
2. ✅ Mock-based unit testing with NSubstitute
3. ✅ HTTP mocking with Imposter
4. ✅ Integration testing patterns
5. ✅ Performance benchmarking
6. ✅ When to use different approaches
7. ✅ Real-world test organization

## 📚 References

- **TUnit:** https://github.com/thomhurst/TUnit
- **NSubstitute:** https://nsubstitute.github.io/
- **Imposter:** https://github.com/themidnightgospel/Imposter
- **.NET Testing Best Practices:** https://docs.microsoft.com/en-us/dotnet/core/testing/

## 🎯 Next Steps

1. ✅ Run the test suite: `dotnet test`
2. ✅ Review domain tests for business logic patterns
3. ✅ Study NSubstitute mocks in service tests
4. ✅ Read conversion guide: `dotnet script ConvertToImposter.csx`
5. ✅ Add Imposter server for integration tests
6. ✅ Run benchmarks to compare approaches
7. ✅ Apply patterns to your own projects

## 💡 Key Insights

### When to Use NSubstitute:
- Unit tests of business logic
- Fast test suites
- Testing interfaces/contracts
- Isolated component testing

### When to Use Imposter:
- Integration tests
- Testing HTTP behavior
- Simulating failures/timeouts
- Testing multiple clients
- Validating serialization

### Hybrid Approach (Recommended):
```
Test Pyramid:
━━━━━━━━━━━━━━━━━━
│  E2E Tests     │  (Few, slow, real infrastructure)
├────────────────┤
│  Integration   │  (Some, medium speed, Imposter)
│  Tests         │
├────────────────┤
│  Unit Tests    │  (Many, fast, NSubstitute)
│  (NSubstitute) │
━━━━━━━━━━━━━━━━━━
```

---

**Happy Testing! 🚀**
