#!/usr/bin/env dotnet-script
/**
 * CloudPizza: nSubstitute to Imposter HTTP Mocking Conversion Guide
 * 
 * This script demonstrates converting from nSubstitute (in-memory mocking)
 * to Imposter (HTTP server mocking). Shows the power of single-file C# apps.
 * 
 * Run with: dotnet script ConvertToImposter.csx
 * 
 * Benefits of Imposter:
 * - Mock actual HTTP servers
 * - Test network failures, timeouts, slow responses
 * - Verify request/response payloads
 * - Simulate real-world scenarios
 * - Better integration testing
 * 
 * See: https://github.com/themidnightgospel/Imposter
 */

#r "nuget: System.Net.Http, 4.3.4"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;

// ============================================================================
// BEFORE: nSubstitute Pattern
// ============================================================================

var nsubstituteExample = @"
// NSubstitute - In-memory mocking of interfaces
public interface IOrderService
{
    Task<Order> GetOrderAsync(Guid id);
    Task<bool> CreateOrderAsync(CreateOrderRequest request);
}

[Test]
public async Task GetOrder_WithMock()
{
    // Arrange
    var mockService = Substitute.For<IOrderService>();
    var expectedOrder = new Order { Id = Guid.NewGuid(), CustomerName = ""John"" };
    
    mockService.GetOrderAsync(Arg.Any<Guid>())
        .Returns(Task.FromResult(expectedOrder));

    // Act
    var result = await mockService.GetOrderAsync(Guid.NewGuid());

    // Assert
    Assert.AreEqual(expectedOrder.Id, result.Id);
    mockService.Received(1).GetOrderAsync(Arg.Any<Guid>());
}

Pros:
  ✓ Fast (no network)
  ✓ Easy to setup
  ✓ Good for unit tests
  ✗ Doesn't test real HTTP behavior
  ✗ Can't test network failures
  ✗ Can't test timeouts
  ✗ Can't verify raw HTTP payloads
";

// ============================================================================
// AFTER: Imposter HTTP Mocking Pattern
// ============================================================================

var imposterExample = @"
// Imposter - HTTP server mocking
// Visit: https://github.com/themidnightgospel/Imposter

[Test]
public async Task GetOrder_WithHttpMock()
{
    // Arrange - Use Imposter server instead of mocks
    using var client = new HttpClient();
    client.BaseAddress = new Uri(""http://localhost:8080"");
    
    // Imposter would be configured to return:
    // GET /api/orders/{id} -> 200 OK with JSON response

    // Act
    var response = await client.GetAsync(""/api/orders/123"");
    var json = await response.Content.ReadAsStringAsync();
    var order = JsonSerializer.Deserialize<Order>(json);

    // Assert
    Assert.AreEqual(200, (int)response.StatusCode);
    Assert.AreEqual(""John"", order.CustomerName);
}

Pros:
  ✓ Tests real HTTP behavior
  ✓ Can test network failures
  ✓ Can test timeouts
  ✓ Can verify raw payloads
  ✓ Better integration testing
  ✓ Reusable across test suites
  ✓ Can test multiple clients (e.g., HttpClient AND WebSockets)
  ✗ Slower (network overhead)
  ✗ Requires running separate server
  ✗ More complex setup
";

// ============================================================================
// CONVERSION PATTERNS
// ============================================================================

var conversionPatterns = new Dictionary<string, (string nsubstitute, string imposter)>
{
    ["Simple Return"] = (
        nsubstitute: @"mock.GetAsync(url).Returns(response)",
        imposter: @"GET /endpoint -> 200 { ""data"": ""value"" }"
    ),
    
    ["Conditional Return"] = (
        nsubstitute: @"mock.GetAsync(Arg.Is(url)).Returns(response)",
        imposter: @"GET /endpoint?param=value -> 200 { ""filtered"": true }"
    ),
    
    ["Error Response"] = (
        nsubstitute: @"mock.GetAsync(url).Throws<HttpRequestException>()",
        imposter: @"GET /endpoint -> 500 { ""error"": ""Server error"" }"
    ),
    
    ["Timeout Simulation"] = (
        nsubstitute: @"// Can't simulate timeout with nSubstitute",
        imposter: @"GET /slow -> delay 5000ms then 200 OK"
    ),
    
    ["Multiple Responses"] = (
        nsubstitute: @"mock.GetAsync(url).Returns(resp1, resp2, resp3)",
        imposter: @"GET /endpoint -> 200 OK (first call)
                   GET /endpoint -> 404 Not Found (second call)
                   GET /endpoint -> 500 Error (third call)"
    ),
    
    ["Request Verification"] = (
        nsubstitute: @"mock.Received(1).PostAsync(Arg.Is(url), Arg.Any<HttpContent>())",
        imposter: @"POST /endpoint recorded -> verify with HttpClient assertions"
    ),
    
    ["Multiple Services"] = (
        nsubstitute: @"var mockService1 = Substitute.For<IService1>();
                   var mockService2 = Substitute.For<IService2>();",
        imposter: @"Single Imposter server mocks multiple endpoints:
                   GET /service1/endpoint
                   POST /service2/endpoint"
    )
};

// ============================================================================
// STEP-BY-STEP CONVERSION GUIDE
// ============================================================================

var conversionSteps = @"
STEP 1: Identify nSubstitute Usage
---------------------------------
Search for:
  - Substitute.For<T>()
  - .Returns(...) calls
  - .Received(...) verification
  - .Throws(...) error simulation

STEP 2: Extract Mock Configuration
-----------------------------------
Before (nSubstitute):
    var mock = Substitute.For<IOrderService>();
    mock.GetOrderAsync(1).Returns(Task.FromResult(order));

After (Imposter):
    - Create Imposter config file (imposter-config.yaml)
    - Define API endpoints and responses
    - Host Imposter server: docker run -p 8080:8080 mockservices/imposter:latest

STEP 3: Convert Interface Calls to HTTP
---------------------------------------
Before:
    var result = await mockService.GetOrderAsync(id);

After:
    var result = await httpClient.GetAsync($""/api/orders/{id}"");

STEP 4: Update Assertions
------------------------
Before:
    mockService.Received(1).GetOrderAsync(id);

After:
    Assert.AreEqual(200, (int)response.StatusCode);
    var json = await response.Content.ReadAsStringAsync();
    Assert.Contains(\"orderId\", json);

STEP 5: Handle Async/Await
--------------------------
No change needed! HttpClient already returns Tasks:
    await httpClient.GetAsync(...) // Already async
    
STEP 6: Test Multiple Scenarios
-------------------------------
Imposter allows testing:
  - Happy path (200 OK)
  - Not found (404)
  - Server errors (500)
  - Timeouts (5000ms delay)
  - Throttling (rate limiting)
  - Partial responses
  - Invalid JSON
";

// ============================================================================
// PRACTICAL EXAMPLE: CloudPizza API
// ============================================================================

var cloudPizzaExample = @"
// Original: Using NSubstitute
[Test]
public async Task CreateOrder_WithNSubstitute()
{
    // Arrange
    var mockApiClient = Substitute.For<IOrderApiClient>();
    var request = new CreateOrderRequest { CustomerName = ""John"", Quantity = 2 };
    var response = new OrderResponse { Id = Guid.NewGuid(), Total = 25.99m };
    
    mockApiClient.CreateOrderAsync(Arg.Is(request))
        .Returns(Task.FromResult(response));

    // Act
    var result = await mockApiClient.CreateOrderAsync(request);

    // Assert
    Assert.AreEqual(""John"", result.CustomerName);
}

// Converted: Using Imposter
[Test]
public async Task CreateOrder_WithImposter()
{
    // Arrange
    var client = new HttpClient { BaseAddress = new Uri(""http://localhost:8080"") };
    var request = new CreateOrderRequest { CustomerName = ""John"", Quantity = 2 };
    var content = new StringContent(
        JsonSerializer.Serialize(request),
        System.Text.Encoding.UTF8,
        ""application/json"");

    // Act
    var response = await client.PostAsync(""/api/orders"", content);
    var json = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<OrderResponse>(json);

    // Assert
    Assert.AreEqual(200, (int)response.StatusCode);
    Assert.AreEqual(""John"", result.CustomerName);
}

// Imposter Config (imposter-config.yaml):
/*
endpoints:
  - path: /api/orders
    method: POST
    response:
      statusCode: 200
      content:
        id: ""{uuid()}""
        customerName: ""John""
        quantity: 2
        total: 25.99
        createdAt: ""{now()}""
*/
";

// ============================================================================
// WHEN TO USE WHAT
// ============================================================================

var whenToUse = @"
Use NSubstitute When:
  ✓ Testing pure business logic
  ✓ Testing interface contracts
  ✓ Need fast unit tests
  ✓ Testing with many different scenarios
  ✓ Need fine-grained control of mocks

Use Imposter When:
  ✓ Testing HTTP behavior
  ✓ Integrating with external APIs
  ✓ Testing error scenarios (timeouts, failures)
  ✓ Testing request/response serialization
  ✓ Building integration tests
  ✓ Simulating real-world API behavior
  ✓ Testing multiple clients against same mock

Use Both:
  ✓ Hybrid approach for comprehensive testing
    - Unit tests: NSubstitute (fast)
    - Integration tests: Imposter (realistic)
";

// ============================================================================
// COMMON GOTCHAS
// ============================================================================

var gotchas = @"
Gotcha #1: Async/Await Differences
  NSubstitute: Mock.GetAsync().Returns(Task.FromResult(x))
  Imposter: Already async with HttpClient

Gotcha #2: Timing Issues
  NSubstitute: Instant
  Imposter: Add delays to test timeouts

Gotcha #3: State Management
  NSubstitute: Mocks are isolated per test
  Imposter: Single server (may need reset between tests)

Gotcha #4: Error Handling
  NSubstitute: .Throws(...) for exceptions
  Imposter: HTTP status codes instead

Gotcha #5: Verification
  NSubstitute: .Received() checks
  Imposter: Must check response status/content
";

// ============================================================================
// OUTPUT REPORT
// ============================================================================

Console.WriteLine(""╔════════════════════════════════════════════════════════════╗"");
Console.WriteLine(""║   CloudPizza: nSubstitute → Imposter Conversion Guide    ║"");
Console.WriteLine(""╚════════════════════════════════════════════════════════════╝\n"");

Console.WriteLine(""📋 CONVERSION PATTERNS"");
Console.WriteLine(new string('─', 60));
foreach (var pattern in conversionPatterns)
{
    Console.WriteLine($""\n  Pattern: {pattern.Key}"");
    Console.WriteLine($"    NSubstitute: {pattern.Value.nsubstitute}"");
    Console.WriteLine($"    Imposter:    {pattern.Value.imposter}"");
}

Console.WriteLine(""\n\n📚 STEP-BY-STEP CONVERSION GUIDE"");
Console.WriteLine(new string('─', 60));
Console.WriteLine(conversionSteps);

Console.WriteLine(""\n\n💡 PRACTICAL EXAMPLE"");
Console.WriteLine(new string('─', 60));
Console.WriteLine(cloudPizzaExample);

Console.WriteLine(""\n\n🎯 WHEN TO USE WHAT"");
Console.WriteLine(new string('─', 60));
Console.WriteLine(whenToUse);

Console.WriteLine(""\n\n⚠️  COMMON GOTCHAS"");
Console.WriteLine(new string('─', 60));
Console.WriteLine(gotchas);

Console.WriteLine(""\n\n📖 BEFORE & AFTER"");
Console.WriteLine(new string('─', 60));
Console.WriteLine(nsubstituteExample);
Console.WriteLine(imposterExample);

Console.WriteLine(""\n\n✅ NEXT STEPS"");
Console.WriteLine(new string('─', 60));
Console.WriteLine(@"
1. Install Imposter:
   https://github.com/themidnightgospel/Imposter

2. Create Imposter config (imposter-config.yaml):
   endpoints:
     - path: /api/orders
       method: GET
       response:
         statusCode: 200
         content: { id: 1, customerName: ""John"" }

3. Start Imposter server:
   docker run -p 8080:8080 -v ./config:/imposter mockservices/imposter:latest

4. Update tests to use HttpClient:
   var client = new HttpClient { BaseAddress = new Uri(""http://localhost:8080"") };
   var response = await client.GetAsync(""/api/orders/1"");

5. Run tests:
   dotnet test

🎉 You've successfully migrated from nSubstitute to Imposter HTTP mocking!
");

// ============================================================================
// METRICS & SUMMARY
// ============================================================================

Console.WriteLine(""\n\n📊 CONVERSION METRICS"");
Console.WriteLine(new string('─', 60));
var metrics = new
{
    ConversionPatterns = conversionPatterns.Count,
    Gotchas = 5,
    StepsToConvert = 6,
    Examples = 3,
    Benefits = new[] { ""Real HTTP testing"", ""Network failure simulation"", ""Timeout testing"", ""Multi-service mocking"" }
};

Console.WriteLine($""  Total Patterns: {metrics.ConversionPatterns}"");
Console.WriteLine($""  Common Gotchas: {metrics.Gotchas}"");
Console.WriteLine($""  Conversion Steps: {metrics.StepsToConvert}"");
Console.WriteLine($""  Example Scenarios: {metrics.Examples}"");
Console.WriteLine($""  Key Benefits:"");
foreach (var benefit in metrics.Benefits)
{
    Console.WriteLine($""    • {benefit}"");
}

Console.WriteLine(""

🎓 Learn More:
  - NSubstitute: https://nsubstitute.github.io/
  - Imposter: https://github.com/themidnightgospel/Imposter
  - HTTP Testing: https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient

This single-file script demonstrates the power of C# scripting! 🚀
"");
