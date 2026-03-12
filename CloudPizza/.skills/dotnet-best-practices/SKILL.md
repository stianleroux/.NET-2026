# .NET 10 Best Practices - Agent Skill

## Overview
This skill provides guidance on modern .NET 10 development best practices, focusing on Clean Architecture, SOLID principles, and the latest language features.

## When to Use This Skill
- Building new .NET 10 applications
- Refactoring existing .NET code to modern standards
- Code reviews for .NET projects
- Architecture decisions for .NET applications
- Implementing design patterns in .NET

## Core Principles

### SOLID Principles

#### Single Responsibility Principle (SRP)
Each class should have one reason to change.

**Good:**
```csharp
// Separate concerns into focused classes
public sealed class OrderValidator
{
    public Result<bool> Validate(Order order) { ... }
}

public sealed class OrderRepository
{
    public async Task<Order> SaveAsync(Order order, CancellationToken ct) { ... }
}

public sealed class OrderNotificationService
{
    public async Task NotifyAsync(Order order, CancellationToken ct) { ... }
}
```

**Bad:**
```csharp
// God class doing too much
public class OrderService
{
    public bool Validate(Order order) { ... }
    public void Save(Order order) { ... }
    public void Notify(Order order) { ... }
    public void SendEmail(Order order) { ... }
    public void LogOrder(Order order) { ... }
}
```

#### Open/Closed Principle (OCP)
Open for extension, closed for modification.

**Good:**
```csharp
public interface IOrderProcessor
{
    Task ProcessAsync(Order order, CancellationToken ct);
}

// Extend behavior without modifying existing code
public sealed class StandardOrderProcessor : IOrderProcessor { ... }
public sealed class PriorityOrderProcessor : IOrderProcessor { ... }
public sealed class BulkOrderProcessor : IOrderProcessor { ... }
```

#### Liskov Substitution Principle (LSP)
Derived classes must be substitutable for their base classes.

**Good:**
```csharp
public interface INotificationService
{
    Task SendAsync(string message, CancellationToken ct);
}

// All implementations honor the contract
public sealed class EmailNotificationService : INotificationService { ... }
public sealed class SmsNotificationService : INotificationService { ... }
```

#### Interface Segregation Principle (ISP)
Clients shouldn't depend on interfaces they don't use.

**Good:**
```csharp
// Focused interfaces
public interface IOrderReader
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct);
}

public interface IOrderWriter
{
    Task SaveAsync(Order order, CancellationToken ct);
}
```

**Bad:**
```csharp
// Fat interface forcing implementations to provide unused methods
public interface IOrderRepository
{
    Task<Order> GetById(OrderId id);
    Task<List<Order>> GetAll();
    Task Save(Order order);
    Task Delete(OrderId id);
    Task<int> Count();
    Task<bool> Exists(OrderId id);
    // ... many more methods
}
```

#### Dependency Inversion Principle (DIP)
Depend on abstractions, not concretions.

**Good:**
```csharp
// Depend on abstraction
public sealed class OrderService(IOrderRepository repository, ILogger<OrderService> logger)
{
    public async Task ProcessAsync(Order order, CancellationToken ct)
    {
        await repository.SaveAsync(order, ct);
        logger.LogInformation("Order {OrderId} processed", order.Id);
    }
}
```

### Clean Architecture

#### Layer Separation
```
┌─────────────────────────────────────┐
│  Presentation (Web, API)            │  ← Framework, UI
├─────────────────────────────────────┤
│  Application (Use Cases, Services)  │  ← Business workflows
├─────────────────────────────────────┤
│  Domain (Entities, Business Logic)  │  ← Core business rules
├─────────────────────────────────────┤
│  Infrastructure (Data, External)    │  ← Database, APIs, Files
└─────────────────────────────────────┘
```

#### Dependency Rules
- Inner layers don't know about outer layers
- Domain has no dependencies
- Infrastructure depends on Domain (not vice versa)
- Use interfaces to invert dependencies

**Good:**
```csharp
// Domain - no dependencies
namespace CloudBurger.Domain;

public sealed class Order
{
    public OrderId Id { get; private set; }
    // Rich domain logic here
}

// Infrastructure - depends on Domain
namespace CloudBurger.Infrastructure;

public sealed class OrderRepository(BurgerDbContext context)
{
    public async Task SaveAsync(Order order, CancellationToken ct)
    {
        context.Orders.Add(order);
        await context.SaveChangesAsync(ct);
    }
}
```

### Modern .NET 10 Language Features

#### Primary Constructors
**Use for:** DI, immutable classes, simple initialization

```csharp
// Good - concise DI
public sealed class OrderService(
    IOrderRepository repository,
    ILogger<OrderService> logger)
{
    public async Task CreateAsync(Order order)
    {
        logger.LogInformation("Creating order");
        await repository.SaveAsync(order);
    }
}
```

#### Required Members
**Use for:** DTOs, ensuring initialization

```csharp
// Good - compiler enforces initialization
public sealed record CreateOrderRequest
{
    public required string CustomerName { get; init; }
    public required string BurgerType { get; init; }
    public required int Quantity { get; init; }
}
```

#### File-scoped Namespaces
**Use always** - reduces indentation

```csharp
// Good
namespace CloudBurger.Domain;

public sealed class Order { ... }

// Bad
namespace CloudBurger.Domain
{
    public sealed class Order { ... }
}
```

#### Records
**Use for:** Value objects, DTOs, immutable data

```csharp
// Good - immutable by default
public sealed record OrderId
{
    public required Guid Value { get; init; }
}

public sealed record OrderDto
{
    public required string OrderId { get; init; }
    public required string CustomerName { get; init; }
}
```

#### Strongly-typed IDs
**Always use** - prevents primitive obsession

```csharp
// Good
public readonly record struct OrderId
{
    public required Guid Value { get; init; }
    public static OrderId New() => new(Guid.NewGuid());
}

public sealed class Order
{
    public OrderId Id { get; private set; }  // Type-safe!
}

// Bad
public sealed class Order
{
    public Guid Id { get; set; }  // Can be confused with other GUIDs
}
```

#### Result Pattern
**Use for:** Business logic errors (not exceptions)

```csharp
// Good - explicit error handling
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }
    
    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
}

public Result<Order> CreateOrder(string customer, BurgerType burger, int quantity)
{
    if (quantity < 1)
        return Result<Order>.Failure("Quantity must be positive");
    
    var order = Order.Create(customer, burger, quantity);
    return Result<Order>.Success(order);
}
```

### Minimal APIs Best Practices

#### Route Groups
**Use for:** Organizing related endpoints

```csharp
var api = app.MapGroup("/api").WithOpenApi();
var orders = api.MapGroup("/orders").WithTags("Orders");

orders.MapPost("/", CreateOrder);
orders.MapGet("/", GetOrders);
orders.MapGet("/{id}", GetOrderById);
```

#### Typed Results
**Always use** - type-safe responses

```csharp
// Good
public static async Task<Results<Ok<OrderDto>, NotFound, ValidationProblem>> GetOrder(
    OrderId id,
    IOrderRepository repository,
    CancellationToken ct)
{
    var order = await repository.GetByIdAsync(id, ct);
    
    if (order is null)
        return TypedResults.NotFound();
    
    return TypedResults.Ok(MapToDto(order));
}
```

#### Feature-based Organization
**Use for:** Vertical slices

```
Features/
├── Orders/
│   ├── OrderEndpoints.cs
│   ├── CreateOrder.cs
│   ├── GetOrders.cs
│   └── OrderMappings.cs
├── Payments/
│   └── ...
```

### Async Best Practices

#### Always Pass CancellationToken
```csharp
// Good
public async Task<List<Order>> GetOrdersAsync(CancellationToken ct)
{
    return await _dbContext.Orders.ToListAsync(ct);
}
```

#### Use ValueTask for Hot Paths
```csharp
// Good - for frequently synchronous paths
public async ValueTask<Order?> GetCachedOrderAsync(OrderId id)
{
    if (_cache.TryGetValue(id, out var order))
        return order;  // Synchronous path
    
    return await LoadFromDatabaseAsync(id);  // Async path
}
```

#### Async Streams for Streaming Data
```csharp
// Good - efficient streaming
public async IAsyncEnumerable<Order> StreamOrdersAsync(
    [EnumeratorCancellation] CancellationToken ct = default)
{
    await foreach (var order in _dbContext.Orders.AsAsyncEnumerable().WithCancellation(ct))
    {
        yield return order;
    }
}
```

### Domain-Driven Design

#### Rich Domain Models (Not Anemic)
```csharp
// Good - behavior in the domain
public sealed class Order
{
    public OrderId Id { get; private set; }
    public Money TotalPrice { get; private set; }
    private List<OrderLine> _lines = new();
    
    public void AddLine(BurgerType burger, int quantity)
    {
        if (quantity < 1)
            throw new DomainException("Quantity must be positive");
        
        _lines.Add(new OrderLine(burger, quantity));
        RecalculateTotal();
    }
    
    private void RecalculateTotal()
    {
        TotalPrice = _lines.Sum(l => l.Price);
    }
}

// Bad - anemic model (just properties)
public class Order
{
    public Guid Id { get; set; }
    public decimal TotalPrice { get; set; }
    public List<OrderLine> Lines { get; set; }
}
```

#### Factory Methods for Creation
```csharp
// Good - controlled creation
public sealed class Order
{
    private Order() { }  // Private constructor
    
    public static Order Create(string customer, BurgerType burger, int quantity)
    {
        // Validation and business rules
        if (string.IsNullOrEmpty(customer))
            throw new ArgumentException("Customer required");
        
        return new Order
        {
            Id = OrderId.New(),
            CustomerName = customer,
            // ...
        };
    }
}
```

### Dependency Injection

#### Lifetime Management
```csharp
// Singleton - stateless services, caching
services.AddSingleton<IMemoryCache, MemoryCache>();

// Scoped - per-request, DbContext
services.AddScoped<IBurgerDbContext, BurgerDbContext>();

// Transient - lightweight, stateless
services.AddTransient<IOrderValidator, OrderValidator>();
```

#### Avoid Service Locator Pattern
```csharp
// Bad
public class OrderService
{
    public void Process(IServiceProvider services)
    {
        var repo = services.GetRequiredService<IOrderRepository>();
        // ...
    }
}

// Good
public sealed class OrderService(IOrderRepository repository)
{
    public void Process()
    {
        // Use injected dependency
    }
}
```

### Error Handling

#### Use Result Pattern for Business Logic
```csharp
// Good
public Result<Order> CreateOrder(CreateOrderRequest request)
{
    if (request.Quantity < 1)
        return Result<Order>.Failure("Invalid quantity");
    
    var order = Order.Create(request.CustomerName, request.BurgerType, request.Quantity);
    return Result<Order>.Success(order);
}
```

#### Exceptions for Infrastructure Failures
```csharp
// Good - infrastructure failures throw
public async Task<Order> LoadOrderAsync(OrderId id)
{
    try
    {
        return await _dbContext.Orders.FindAsync(id);
    }
    catch (SqlException ex)
    {
        _logger.LogError(ex, "Database error loading order");
        throw;
    }
}
```

### Testing Patterns

#### Use Test Data Builders
```csharp
public sealed class OrderBuilder
{
    private string _customer = "Test Customer";
    private BurgerType _burger = BurgerType.SmashBurger;
    private int _quantity = 1;
    
    public OrderBuilder WithCustomer(string name)
    {
        _customer = name;
        return this;
    }
    
    public Order Build() => Order.Create(_customer, _burger, _quantity);
}

// Usage
var order = new OrderBuilder()
    .WithCustomer("John")
    .WithQuantity(5)
    .Build();
```

## Anti-Patterns to Avoid

### ❌ Magic Strings
```csharp
// Bad
if (order.Status == "pending") { }

// Good
if (order.Status == OrderStatus.Pending) { }
```

### ❌ Static State
```csharp
// Bad
public static class OrderCache
{
    public static List<Order> Orders = new();
}

// Good
public sealed class OrderCache
{
    private readonly ConcurrentDictionary<OrderId, Order> _cache = new();
}
```

### ❌ God Classes
```csharp
// Bad - does everything
public class OrderManager
{
    public void Validate() { }
    public void Save() { }
    public void Email() { }
    public void Log() { }
    public void Audit() { }
}
```

### ❌ Primitive Obsession
```csharp
// Bad
public void ProcessOrder(Guid orderId, Guid customerId, Guid productId) { }

// Good
public void ProcessOrder(OrderId orderId, CustomerId customerId, ProductId productId) { }
```

## Quick Reference

### Code Smells to Watch For
- [ ] Methods longer than 20 lines
- [ ] Classes with more than 7 dependencies
- [ ] Deep nesting (>3 levels)
- [ ] Duplicate code
- [ ] Long parameter lists (>4 parameters)
- [ ] Comments explaining "what" instead of "why"

### Daily Checklist
- [ ] Used strongly-typed IDs instead of primitives
- [ ] Applied Result pattern for business errors
- [ ] Passed CancellationToken to async methods
- [ ] Used records for immutable DTOs
- [ ] Applied primary constructors for DI
- [ ] Used file-scoped namespaces
- [ ] Kept classes focused (SRP)
- [ ] Depended on abstractions (DIP)

## Resources
- [.NET Design Guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/)
- [C# Coding Conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Clean Architecture by Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

## Examples Repository
See the CloudBurger demo application for real-world examples of all these practices in action.
