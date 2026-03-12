# Validation and Error Handling Architecture

## 📋 Table of Contents
- [Overview](#overview)
- [Validation Strategy](#validation-strategy)
- [The Result Pattern](#the-result-pattern)
- [Pattern Matching for Validation](#pattern-matching-for-validation)
- [FluentValidation for Value Objects](#fluentvalidation-for-value-objects)
- [Data Annotations for API DTOs](#data-annotations-for-api-dtos)
- [Code Examples](#code-examples)
- [Best Practices](#best-practices)
- [Migration Guide](#migration-guide)

---

## 🎯 Overview

CloudBurger uses a **multi-layered validation strategy** that eliminates exception-driven control flow for business logic while leveraging .NET 10's built-in validation features where appropriate.

### Key Principles

✅ **No Exceptions for Business Rules** - Use Result pattern for expected validation failures  
✅ **Explicit Error Handling** - Callers must handle both success and failure cases  
✅ **Type Safety** - Compiler enforces handling of all cases  
✅ **Performance** - Avoids exception overhead for validation  
✅ **Structured Errors** - Field-level validation errors for better UX  

---

## 🎨 Validation Strategy

We use **three different validation approaches** depending on the layer and purpose:

| Layer | Approach | Use Case | Example |
|-------|----------|----------|---------|
| **API DTOs** | Data Annotations | HTTP request validation | `CreateOrderRequest` |
| **Domain Value Objects** | FluentValidation + Result | Complex validation logic | `OrderId` |
| **Domain Entities** | Pattern Matching + Result | Business rule validation | `Order.Create()` |

---

## 🎁 The Result Pattern

### What is the Result Pattern?

The Result pattern is a functional programming technique that makes success/failure explicit in the type system, avoiding exceptions for control flow.

### Result<T> Implementation

```csharp
// Location: src/CloudPizza.Shared/Common/Result.cs

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; }
    public string Error { get; }
    public Dictionary<string, string[]>? ValidationErrors { get; }

    // Factory methods
    public static Result<T> Success(T value);
    public static Result<T> Failure(string error);
    public static Result<T> ValidationFailure(string error, Dictionary<string, string[]> validationErrors);
}
```

### Why Use Result Pattern?

**❌ Before (Exception-Driven):**
```csharp
public static Order Create(string name, BurgerType burger, int qty)
{
    if (string.IsNullOrEmpty(name))
        throw new ArgumentException("Name required");
    
    if (qty < 1)
        throw new ArgumentException("Invalid quantity");
    
    return new Order { Name = name, ... };
}

// Usage
try
{
    var order = Order.Create(customerName, burger, quantity);
    // Success path
}
catch (ArgumentException ex)
{
    // Error handling - forces try/catch everywhere
}
```

**✅ After (Result Pattern):**
```csharp
public static Result<Order> Create(string name, BurgerType burger, int qty)
{
    var nameValidation = name switch
    {
        null or "" => Result<string>.ValidationFailure(
            "Name validation failed",
            new Dictionary<string, string[]> { ["CustomerName"] = ["Name required"] }),
        _ => Result<string>.Success(name)
    };

    if (nameValidation.IsFailure)
        return Result<Order>.ValidationFailure(
            nameValidation.Error, 
            nameValidation.ValidationErrors!);

    return Result<Order>.Success(new Order { Name = name, ... });
}

// Usage
var orderResult = Order.Create(customerName, burger, quantity);

if (orderResult.IsFailure)
{
    // Handle validation errors explicitly
    return TypedResults.ValidationProblem(orderResult.ValidationErrors);
}

var order = orderResult.Value;
// Continue with success path
```

### Benefits

✅ **Compile-Time Safety** - Compiler forces handling of both cases  
✅ **No Hidden Control Flow** - No invisible exceptions  
✅ **Better Performance** - No exception stack unwinding  
✅ **Testability** - Easy to test both success and failure paths  
✅ **Composability** - Can chain operations with `Map()`, `Bind()`  

---

## 🎭 Pattern Matching for Validation

C# 14 pattern matching provides elegant, declarative validation logic.

### Domain Entity Validation Example

```csharp
// Location: src/CloudPizza.Shared/Domain/Order.cs

public static Result<Order> Create(string customerName, BurgerType burgerType, int quantity)
{
    // Validate customer name using pattern matching
    var nameValidation = customerName switch
    {
        null or "" => Result<string>.ValidationFailure(
            "Customer name validation failed",
            new Dictionary<string, string[]> 
            { 
                ["CustomerName"] = ["Customer name is required"] 
            }),
        { Length: < 2 } => Result<string>.ValidationFailure(
            "Customer name validation failed",
            new Dictionary<string, string[]> 
            { 
                ["CustomerName"] = ["Customer name must be at least 2 characters"] 
            }),
        { Length: > 100 } => Result<string>.ValidationFailure(
            "Customer name validation failed",
            new Dictionary<string, string[]> 
            { 
                ["CustomerName"] = ["Customer name cannot exceed 100 characters"] 
            }),
        _ => Result<string>.Success(customerName.Trim())
    };

    if (nameValidation.IsFailure)
        return Result<Order>.ValidationFailure(
            nameValidation.Error, 
            nameValidation.ValidationErrors!);

    // Validate quantity using range patterns
    var quantityValidation = quantity switch
    {
        < 1 => Result<int>.ValidationFailure(
            "Quantity validation failed",
            new Dictionary<string, string[]> 
            { 
                ["Quantity"] = ["Quantity must be at least 1"] 
            }),
        > 50 => Result<int>.ValidationFailure(
            "Quantity validation failed",
            new Dictionary<string, string[]> 
            { 
                ["Quantity"] = ["Cannot order more than 50 burgers at once"] 
            }),
        _ => Result<int>.Success(quantity)
    };

    if (quantityValidation.IsFailure)
        return Result<Order>.ValidationFailure(
            quantityValidation.Error, 
            quantityValidation.ValidationErrors!);

    // All validations passed - create the order
    return Result<Order>.Success(new Order
    {
        Id = OrderId.New(),
        CustomerName = nameValidation.Value,
        BurgerType = burgerType,
        Quantity = quantity,
        CreatedAtUtc = DateTime.UtcNow,
        TotalPrice = burgerType.GetPrice() * quantity
    });
}
```

### Pattern Matching Features Used

```csharp
// Null/empty patterns
value switch
{
    null or "" => Failure("Required"),
    _ => Success(value)
}

// Property patterns
customerName switch
{
    { Length: < 2 } => Failure("Too short"),
    { Length: > 100 } => Failure("Too long"),
    _ => Success(customerName)
}

// Range patterns
quantity switch
{
    < 1 => Failure("Too small"),
    > 50 => Failure("Too large"),
    _ => Success(quantity)
}

// Boolean patterns
Enum.IsDefined(burgerType) switch
{
    false => Failure("Invalid enum value"),
    true => Success(burgerType)
}
```

---

## 🔍 FluentValidation for Value Objects

Use FluentValidation for complex validation logic in value objects and domain primitives.

### Implementation Example

```csharp
// Location: src/CloudPizza.Shared/Domain/OrderId.cs

using FluentValidation;
using CloudBurger.Shared.Common;

public readonly record struct OrderId
{
    public Guid Value { get; init; }

    private OrderId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new OrderId with FluentValidation.
    /// </summary>
    public static Result<OrderId> Create(Guid value)
    {
        var validator = new OrderIdValidator();
        var validationResult = validator.Validate(value);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return Result<OrderId>.ValidationFailure(
                "OrderId validation failed",
                errors
            );
        }

        return Result<OrderId>.Success(new OrderId(value));
    }

    public static OrderId New()
    {
        return new(Guid.NewGuid());
    }
}

/// <summary>
/// FluentValidation validator for OrderId.
/// </summary>
public sealed class OrderIdValidator : AbstractValidator<Guid>
{
    public OrderIdValidator()
    {
        RuleFor(guid => guid)
            .NotEmpty()
            .WithMessage("OrderId cannot be empty")
            .WithName("Value");

        RuleFor(guid => guid)
            .Must(guid => guid != Guid.Empty)
            .WithMessage("OrderId must be a non-empty GUID")
            .WithName("Value");
    }
}
```

### When to Use FluentValidation

✅ **Value Objects** - Complex validation logic  
✅ **Reusable Validators** - Need to share validation rules  
✅ **Conditional Validation** - Rules depend on other properties  
✅ **Custom Validators** - Need to implement complex business rules  

---

## 📝 Data Annotations for API DTOs

Keep Data Annotations on API contracts because .NET 10 Minimal APIs provide **automatic validation**.

### API Contract Example

```csharp
// Location: src/CloudPizza.Shared/Contracts/OrderContracts.cs

using System.ComponentModel.DataAnnotations;

public sealed record CreateOrderRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public required string CustomerName { get; init; }

    [Required]
    public required string BurgerType { get; init; }

    [Range(1, 50)]
    public required int Quantity { get; init; }
}
```

### Automatic Validation in Minimal APIs

```csharp
// Location: src/CloudPizza.Api/Features/Orders/OrderEndpoints.cs

// .NET 10 automatically validates CreateOrderRequest!
private static async Task<Results<Created<CreateOrderResponse>, ValidationProblem>> 
    CreateOrderAsync(
        CreateOrderRequest request,  // ← Automatically validated
        BurgerDbContext dbContext,
        CancellationToken cancellationToken)
{
    // If validation fails, .NET returns 400 Bad Request automatically
    // with structured ValidationProblem response

    // If we get here, Data Annotations passed
    // Now apply domain validation using Result pattern
    var orderResult = Order.Create(
        request.CustomerName, 
        burgerType, 
        request.Quantity);

    if (orderResult.IsFailure)
    {
        return TypedResults.ValidationProblem(orderResult.ValidationErrors);
    }

    // Success path
    var order = orderResult.Value;
    // ... save to database
}
```

### Why Keep Data Annotations?

✅ **Zero Configuration** - Automatic validation in Minimal APIs  
✅ **Standard HTTP Responses** - Consistent 400 Bad Request with ProblemDetails  
✅ **OpenAPI Integration** - Swagger/Scalar documentation generation  
✅ **Client Validation** - Blazor can use same attributes for client-side validation  

---

## 💻 Code Examples

### Full Endpoint Example

```csharp
// Location: src/CloudPizza.Api/Features/Orders/OrderEndpoints.cs

private static async Task<Results<Created<CreateOrderResponse>, ValidationProblem, ProblemHttpResult>> 
    CreateOrderAsync(
        CreateOrderRequest request,
        BurgerDbContext dbContext,
        CancellationToken cancellationToken)
{
    // Step 1: Data Annotations validation (automatic)
    // If request fails Data Annotations, .NET returns 400 automatically

    // Step 2: Parse burger type
    if (!Enum.TryParse<BurgerType>(request.BurgerType, ignoreCase: true, out var burgerType))
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["BurgerType"] = [$"Invalid burger type. Valid values: {string.Join(", ", Enum.GetNames<BurgerType>())}"]
        });
    }

    // Step 3: Domain validation using Result pattern
    var orderResult = Order.Create(request.CustomerName, burgerType, request.Quantity);

    // Step 4: Handle validation failures
    if (orderResult.IsFailure)
    {
        var validationErrors = orderResult.ValidationErrors ?? new Dictionary<string, string[]>
        {
            ["Order"] = [orderResult.Error]
        };
        
        return TypedResults.ValidationProblem(validationErrors);
    }

    // Step 5: Success - save to database
    var order = orderResult.Value;
    dbContext.Orders.Add(order);
    await dbContext.SaveChangesAsync(cancellationToken);

    // Step 6: Return 201 Created response
    var response = new CreateOrderResponse
    {
        OrderId = order.Id.ToString(),
        CustomerName = order.CustomerName,
        BurgerType = order.BurgerType.GetDisplayName(),
        Quantity = order.Quantity,
        TotalPrice = order.TotalPrice,
        CreatedAtUtc = order.CreatedAtUtc
    };

    return TypedResults.Created($"/api/orders/{order.Id}", response);
}
```

### Service Layer Example

```csharp
// Location: src/CloudPizza.Infrastructure/Services/QrCodeService.cs

public static Result<byte[]> GenerateQrCode(string url, int pixelsPerModule = 20)
{
    // Validation using pattern matching
    var urlValidation = url switch
    {
        null or "" or { Length: 0 } => Result<string>.ValidationFailure(
            "URL validation failed",
            new Dictionary<string, string[]> { ["url"] = ["URL is required"] }),
        { } when string.IsNullOrWhiteSpace(url) => Result<string>.ValidationFailure(
            "URL validation failed",
            new Dictionary<string, string[]> { ["url"] = ["URL cannot be empty"] }),
        _ => Result<string>.Success(url)
    };

    if (urlValidation.IsFailure)
        return Result<byte[]>.ValidationFailure(
            urlValidation.Error, 
            urlValidation.ValidationErrors!);

    // Infrastructure operations wrapped in try-catch
    try
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);

        var bytes = qrCode.GetGraphic(pixelsPerModule);
        return Result<byte[]>.Success(bytes);
    }
    catch (Exception ex)
    {
        // Infrastructure failures use Failure, not ValidationFailure
        return Result<byte[]>.Failure($"Failed to generate QR code: {ex.Message}");
    }
}
```

---

## ✅ Best Practices

### DO ✅

1. **Use ValidationFailure for validation errors**
   ```csharp
   return Result<Order>.ValidationFailure(
       "Validation failed",
       new Dictionary<string, string[]> { ["Field"] = ["Error message"] }
   );
   ```

2. **Use Failure for infrastructure/unexpected errors**
   ```csharp
   return Result<byte[]>.Failure($"Failed to generate QR: {ex.Message}");
   ```

3. **Use pattern matching for business rules**
   ```csharp
   var validation = value switch
   {
       < 1 => Failure("Too small"),
       > 100 => Failure("Too large"),
       _ => Success(value)
   };
   ```

4. **Keep Data Annotations on API DTOs**
   ```csharp
   public sealed record CreateOrderRequest
   {
       [Required]
       [MinLength(2)]
       public required string Name { get; init; }
   }
   ```

5. **Use FluentValidation for complex value objects**
   ```csharp
   public sealed class EmailValidator : AbstractValidator<string>
   {
       public EmailValidator()
       {
           RuleFor(x => x).EmailAddress();
       }
   }
   ```

### DON'T ❌

1. **Don't throw exceptions for validation**
   ```csharp
   ❌ if (value < 1) throw new ArgumentException("Invalid");
   ✅ if (value < 1) return Result<T>.ValidationFailure(...);
   ```

2. **Don't use exceptions for control flow**
   ```csharp
   ❌ try { var order = Order.Create(...); } catch { }
   ✅ var result = Order.Create(...); if (result.IsFailure) { }
   ```

3. **Don't mix validation styles in same layer**
   ```csharp
   ❌ Domain entity with Data Annotations
   ✅ Domain entity with Result pattern
   ```

4. **Don't ignore Result failures**
   ```csharp
   ❌ var order = Order.Create(...).Value; // Can throw!
   ✅ var result = Order.Create(...);
      if (result.IsSuccess) { var order = result.Value; }
   ```

---

## 🔄 Migration Guide

### Migrating from Exceptions to Result Pattern

**Step 1: Identify exception-throwing code**
```csharp
// Before
public static Order Create(string name, int qty)
{
    if (string.IsNullOrEmpty(name))
        throw new ArgumentException("Name required");
    
    return new Order { Name = name };
}
```

**Step 2: Change return type to Result<T>**
```csharp
// After
public static Result<Order> Create(string name, int qty)
{
    if (string.IsNullOrEmpty(name))
        return Result<Order>.ValidationFailure(
            "Validation failed",
            new Dictionary<string, string[]> { ["Name"] = ["Name required"] }
        );
    
    return Result<Order>.Success(new Order { Name = name });
}
```

**Step 3: Update callers**
```csharp
// Before
try
{
    var order = Order.Create(name, qty);
    await SaveAsync(order);
}
catch (ArgumentException ex)
{
    return BadRequest(ex.Message);
}

// After
var orderResult = Order.Create(name, qty);
if (orderResult.IsFailure)
{
    return TypedResults.ValidationProblem(orderResult.ValidationErrors);
}

var order = orderResult.Value;
await SaveAsync(order);
```

**Step 4: Refactor to pattern matching (optional)**
```csharp
public static Result<Order> Create(string name, int qty)
{
    var nameValidation = name switch
    {
        null or "" => Result<string>.ValidationFailure(
            "Name validation failed",
            new Dictionary<string, string[]> { ["Name"] = ["Required"] }),
        { Length: > 100 } => Result<string>.ValidationFailure(
            "Name validation failed",
            new Dictionary<string, string[]> { ["Name"] = ["Too long"] }),
        _ => Result<string>.Success(name)
    };

    if (nameValidation.IsFailure)
        return Result<Order>.ValidationFailure(
            nameValidation.Error, 
            nameValidation.ValidationErrors!);

    return Result<Order>.Success(new Order { Name = nameValidation.Value });
}
```

---

## 📚 Additional Resources

### Related Documentation
- [Result Pattern explained](https://enterprisecraftsmanship.com/posts/functional-c-handling-failures-input-errors/)
- [C# 14 Pattern Matching](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/patterns)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [Minimal API Validation in .NET 10](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)

### Code Locations

| Component | Location |
|-----------|----------|
| Result<T> | `src/CloudPizza.Shared/Common/Result.cs` |
| Order Domain Entity | `src/CloudPizza.Shared/Domain/Order.cs` |
| OrderId Value Object | `src/CloudPizza.Shared/Domain/OrderId.cs` |
| API DTOs | `src/CloudPizza.Shared/Contracts/OrderContracts.cs` |
| Order Endpoints | `src/CloudPizza.Api/Features/Orders/OrderEndpoints.cs` |
| QR Code Service | `src/CloudPizza.Infrastructure/Services/QrCodeService.cs` |

---

## 🎯 Summary

CloudBurger uses a **layered validation strategy**:

1. **API Layer** → Data Annotations (automatic validation)
2. **Domain Value Objects** → FluentValidation + Result pattern
3. **Domain Entities** → Pattern Matching + Result pattern
4. **Infrastructure** → Try/Catch + Result pattern (Failure, not ValidationFailure)

This approach provides:
- ✅ Compile-time safety
- ✅ No exception-driven flow
- ✅ Better performance
- ✅ Structured error responses
- ✅ Excellent developer experience

---

**Last Updated:** March 2026  
**Author:** CloudBurger Development Team  
**Version:** 1.0
