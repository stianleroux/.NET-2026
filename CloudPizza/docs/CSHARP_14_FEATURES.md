# C# 14 & ASP.NET 10 Features in CloudBurger

This document outlines modern C# 14 and ASP.NET 10 features implemented in CloudBurger, plus opportunities for future enhancement.

## ✅ Already Implemented Features

### 1. **Minimal APIs with Validation** ✅
ASP.NET 10 built-in validation for Minimal APIs

```csharp
// Program.cs
builder.Services.AddValidation();

// OrderContracts.cs
public sealed record CreateOrderRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public required string CustomerName { get; init; }
    
    [Range(1, 50)]
    public required int Quantity { get; init; }
}

// OrderEndpoints.cs - Validation automatic via data annotations
app.MapPost("/api/orders", CreateOrderHandler);
// → Automatic ProblemDetails response if invalid
```

**Benefits:**
- ✅ Declarative validation
- ✅ Automatic ProblemDetails responses
- ✅ Built-in, no FluentValidation needed
- ✅ Works with records and classes
- ✅ Custom validators supported

### 2. **Pattern Matching** ✅
Modern pattern matching throughout codebase

```csharp
// ApiClient.cs - SSE Parser
switch (line)
{
    case var l when l.StartsWith("event:"):
        eventType = l[6..].Trim();
        break;
    case var l when l.StartsWith("data:"):
        data = l[5..].Trim();
        break;
    case "" or null when data is not null:
        // Process message
        break;
}

// Program.cs - Exception handling
return ex switch
{
    ArgumentException ae => Results.BadRequest(ae.Message),
    InvalidOperationException ioe => Results.Conflict(ioe.Message),
    _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
};
```

### 3. **Primary Constructors** ✅
Concise dependency injection

```csharp
// GlobalExceptionHandler.cs
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(...)
    {
        logger.LogError(exception, "Unhandled exception");
    }
}

// QrCodeService.cs
public sealed class QrCodeService(int pixelsPerModule = 5)
{
    public string GenerateQrCodeAsBase64(string data)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        // ...
    }
}
```

### 4. **File-Scoped Namespaces** ✅
Cleaner code structure

```csharp
// Every file starts with
namespace CloudBurger.Features.Orders;

// No closing brace needed
```

### 5. **Required Properties** ✅
Enforce initialization in records

```csharp
public sealed record CreateOrderRequest
{
    public required string CustomerName { get; init; }
    public required string BurgerType { get; init; }
    public required int Quantity { get; init; }
}
// → Compile error if not initialized
```

### 6. **Init-Only Properties** ✅
Immutable value objects

```csharp
public sealed record OrderResponse(
    Guid Id,
    string CustomerName,
    string BurgerType,
    int Quantity,
    DateTime CreatedAtUtc,
    decimal TotalPrice);
    
// → Can set in constructor, never change
```

### 7. **Tuple Deconstruction** ✅
Pattern matching with tuples

```csharp
// Domain validation
var (isValid, error) = ValidateOrder(request);
if (!isValid)
    return Results.BadRequest(error);
```

### 8. **Null-Conditional Operators** ✅
Safe navigation

```csharp
// OrderEndpoints.cs
var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == request.Id);
var response = order?.Id != null 
    ? MapToResponse(order) 
    : null;
```

## 🚀 C# 14 Features Available for Implementation

### 1. **Field Keyword** 🆕 (HIGH PRIORITY)
Simplify property validation without manual backing fields

```csharp
// CURRENT: Order.cs
public sealed class Order
{
    private string _customerName = string.Empty;
    public string CustomerName
    {
        get => _customerName;
        private init => _customerName = value?? throw new ArgumentNullException();
    }
}

// C# 14: Order.cs
public sealed class Order
{
    public string CustomerName
    {
        get => field;
        private init => field = value ?? throw new ArgumentNullException();
    }
}
```

**Use Case:** Domain models with validation in setters

### 2. **Extension Blocks** 🆕 (MEDIUM PRIORITY)
Organize multiple extension types in one block

```csharp
// BEFORE: Multiple extension classes
public static class OrderExtensions
{
    public static OrderResponse ToResponse(this Order order) => ...;
}

public static class OrderQueryExtensions
{
    public static IQueryable<Order> Recent(this IQueryable<Order> orders) => ...;
}

// C# 14: Single organized block
public static class OrderExtensions
{
    extension(Order order)
    {
        public OrderResponse ToResponse()
            => new(order.Id.Value, order.CustomerName, ...);
    }
    
    extension(IQueryable<Order> orders)
    {
        public static IQueryable<Order> Recent()
            => orders.OrderByDescending(o => o.CreatedAtUtc);
    }
}
```

**Use Case:** API mappers and query helpers

### 3. **Partial Constructors** 🆕 (MEDIUM PRIORITY)
Source gen-friendly initialization

```csharp
// BEFORE: Manual initialization call
public partial class OrderConfig
{
    public OrderConfig(string configPath)
    {
        LoadConfig(configPath); // Must remember to call
    }
    
    private partial void LoadConfig(string path);
}

// C# 14: Partial constructor
public partial class OrderConfig
{
    public partial OrderConfig(string configPath); // Auto-generated
}

// In generated file
public partial class OrderConfig
{
    public partial OrderConfig(string configPath)
    {
        // Generated code...
    }
}
```

**Use Case:** Source-generated configuration

### 4. **Null-Conditional Assignment** 🆕 (MEDIUM PRIORITY)
Safe property assignment

```csharp
// BEFORE
var policy = GetPolicy();
if (policy is not null)
{
    policy.CustomerId = customerId;
}

// C# 14
GetPolicy()?.CustomerId = customerId;

// In OrderEndpoints
order?.AssignDeliveryAddress(address);
```

**Use Case:** Defensive API controller code

### 5. **Compound Assignment Operators** 🆕 (LOW PRIORITY)
Performance for value types

```csharp
// Money struct example (not in CloudBurger but useful)
public struct Money(string currency, decimal amount)
{
    public void operator +=(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException();
        Amount += other.Amount;
    }
}

// Usage
var total = new Money("USD", 100);
total += new Money("USD", 50); // No extra object created
```

### 6. **Lambda Parameter Modifiers** 🆕 (LOW PRIORITY)
ref/in/out in lambdas without explicit types

```csharp
// BEFORE
ApplyDiscount apply = (ref decimal price) => price *= 0.9m;

// C# 14 (if type inference works)
ApplyDiscount apply = (ref price) => price *= 0.9m;
```

### 7. **Nameof for Unbound Generics** 🆕 (MEDIUM PRIORITY)
Better diagnostics

```csharp
// BEFORE
logger.LogInformation("Processing {Type}", nameof(List<string>)); // awkward
// Output: List (but had to specify string)

// C# 14
logger.LogInformation("Processing {Type}", nameof(List<>));
// Output: List (cleaner)
```

**Use Case:** Generic type logging

### 8. **Span<T> Implicit Conversions** 🆕 (MEDIUM PRIORITY)
First-class Span support

```csharp
// BEFORE
public decimal CalculateTotal(ReadOnlySpan<Order> orders)
{
    return orders.Sum(o => o.TotalPrice);
}

// Must explicitly cast
var total = CalculateTotal(new Order[] { order1, order2 });

// C# 14: Implicit conversion
var total = CalculateTotal(new[] { order1, order2 }); // Just works!
```

## 🎯 Recommended Implementation Plan

### Phase 1: Quick Wins (Immediate) ✅
- [ ] Already Done - No changes needed!

### Phase 2: High-Impact Features (Next Sprint)
1. **Field Keyword** - Add to Order, OrderId, BurgerType Domain Models
   - Simplify validation logic
   - Remove manual backing fields
   - ~15 min implementation

2. **Span<T> for Performance** - Use in QR code generation
   - Better memory efficiency
   - Working with byte arrays
   - ~10 min implementation

### Phase 3: Code Organization (Following Sprint)
1. **Extension Blocks** - Organize API mappers
   - Better code structure
   - Central OrderResponse mapping
   - ~20 min implementation

2. **Null-Conditional Assignment** - Defensive checks
   - Cleaner null handling
   - Safety in order processing
   - ~10 min implementation

### Phase 4: Advanced Features (Later)
1. **Partial Constructors** - Entity Framework models
2. **Compound Operators** - If introducing value objects
3. **Unbound Generics** - Logging improvements

## 📝 Example: Implementing Field Keyword

```csharp
// BEFORE: Order.cs (Current)
public sealed class Order
{
    private string _customerName = string.Empty;
    
    public string CustomerName
    {
        get => _customerName;
        private init => _customerName = value.Trim();
    }
}

// AFTER: Order.cs (C# 14)
public sealed class Order
{
    public string CustomerName
    {
        get => field;
        private init => field = (value ?? string.Empty).Trim();
    }
    
    public BurgerType BurgerType
    {
        get => field;
        private init => field = value;
    }
    
    public int Quantity
    {
        get => field;
        private init
        {
            if (value < 1) throw new ArgumentException("...");
            if (value > 50) throw new ArgumentException("...");
            field = value;
        }
    }
}
```

## 🎓 ASP.NET 10 Features in Use

### ✅ Minimal APIs
- Route groups with `/api/orders` prefix
- Typed results: `Results<Ok<T>, BadRequest, NotFound>`
- Built-in validation with data annotations
- OpenAPI 3.1 integration

### ✅ Server-Sent Events (SSE)
- Real-time order updates
- PostgreSQL LISTEN/NOTIFY integration
- Async streaming responses

### ✅ Dependency Injection
- Primary constructor injection
- Typed HttpClient registration
- Scoped service management

### ✅ Logging & Diagnostics
- Structured logging with ILogger
- Problem Details RFC 7807
- Exception tracking with trace IDs

### ✅ OpenAPI/Swagger
- Scalar UI for documentation
- Rich endpoint metadata
- Automatic contract generation

## 🚀 Running Example Projects

```bash
# Check current C# version
dotnet --version

# View all C# 14 features in action
dotnet test

# Run benchmarks comparing approaches
dotnet test --filter "Benchmark"

# Check test coverage
dotnet test /p:CollectCoverage=true
```

## 📊 Feature Matrix

| Feature              | CloudBurger | C# Version | Priority |
| -------------------- | ---------- | ---------- | -------- |
| Minimal APIs         | ✅          | 8.0+       | ✅ Done   |
| Validation           | ✅          | 8.0+       | ✅ Done   |
| Pattern Matching     | ✅          | 7.0+       | ✅ Done   |
| Primary Constructors | ✅          | 12.0+      | ✅ Done   |
| Required Properties  | ✅          | 11.0+      | ✅ Done   |
| File-Scoped NS       | ✅          | 10.0+      | ✅ Done   |
| **Field Keyword**    | 🔲          | 14.0+      | 🟡 High   |
| **Extension Blocks** | 🔲          | 14.0+      | 🟡 Medium |
| **Null-Cond Assign** | 🔲          | 14.0+      | 🟡 Medium |
| Partial Constructors | 🔲          | 14.0+      | 🟠 Low    |
| Compound Assign Ops  | 🔲          | 14.0+      | 🟠 Low    |
| Span<T> Implicit     | 🔲          | 14.0+      | 🟡 Medium |

## 🎯 Next Steps

1. **Review** test suite to understand validation patterns
2. **Read** conversion script to learn mocking approaches
3. **Try** field keyword in Order domain model
4. **Experiment** with extension blocks for API mappers
5. **Benchmark** to measure performance improvements

---

**Modern .NET is constantly evolving—keep learning! 🚀**
