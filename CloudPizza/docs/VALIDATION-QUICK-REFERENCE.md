# Validation Quick Reference

## 🎯 Decision Tree: Which Validation Approach?

```
┌─────────────────────────────────────┐
│  What are you validating?           │
└──────────┬──────────────────────────┘
           │
           ├─→ API Request/Response DTO?
           │   └─→ ✅ Use Data Annotations
           │       Example: CreateOrderRequest
           │       [Required, MinLength(2), Range(1, 50)]
           │
           ├─→ Domain Value Object?
           │   └─→ ✅ Use FluentValidation + Result
           │       Example: OrderId, EmailAddress
           │       AbstractValidator<T> + Result<T>.ValidationFailure()
           │
           ├─→ Domain Entity Creation?
           │   └─→ ✅ Use Pattern Matching + Result
           │       Example: Order.Create()
           │       value switch { ... } + Result<T>.ValidationFailure()
           │
           └─→ Infrastructure Operation?
               └─→ ✅ Use Try/Catch + Result
                   Example: QrCodeService
                   try { } catch { } + Result<T>.Failure()
```

## 📋 Cheat Sheet

### Data Annotations (API DTOs)

```csharp
public sealed record CreateOrderRequest
{
    [Required]
    [MinLength(2), MaxLength(100)]
    public required string CustomerName { get; init; }

    [Range(1, 50)]
    public required int Quantity { get; init; }
}
```

**When:** API contracts, automatic validation  
**Returns:** Automatic 400 Bad Request from .NET

---

### Pattern Matching (Domain Entities)

```csharp
public static Result<Order> Create(string name, int qty)
{
    var validation = name switch
    {
        null or "" => Result<string>.ValidationFailure(
            "Validation failed",
            new Dictionary<string, string[]> { ["Name"] = ["Required"] }),
        { Length: < 2 } => Result<string>.ValidationFailure(
            "Validation failed",
            new Dictionary<string, string[]> { ["Name"] = ["Too short"] }),
        _ => Result<string>.Success(name)
    };

    if (validation.IsFailure)
        return Result<Order>.ValidationFailure(
            validation.Error, 
            validation.ValidationErrors!);

    return Result<Order>.Success(new Order { Name = validation.Value });
}
```

**When:** Business rule validation, domain entities  
**Returns:** `Result<T>` with structured errors

---

### FluentValidation (Value Objects)

```csharp
public readonly record struct OrderId
{
    public static Result<OrderId> Create(Guid value)
    {
        var validator = new OrderIdValidator();
        var result = validator.Validate(value);

        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            return Result<OrderId>.ValidationFailure(
                "OrderId validation failed", 
                errors);
        }

        return Result<OrderId>.Success(new OrderId(value));
    }
}

public class OrderIdValidator : AbstractValidator<Guid>
{
    public OrderIdValidator()
    {
        RuleFor(x => x)
            .NotEmpty()
            .WithMessage("Cannot be empty");
    }
}
```

**When:** Complex validation logic, reusable validators  
**Returns:** `Result<T>` with FluentValidation errors

---

## 🔑 Key Result Methods

### Success
```csharp
return Result<Order>.Success(order);
```

### ValidationFailure (Business Rules)
```csharp
return Result<Order>.ValidationFailure(
    "Validation failed",
    new Dictionary<string, string[]> 
    { 
        ["FieldName"] = ["Error message"] 
    });
```

### Failure (Infrastructure Errors)
```csharp
return Result<byte[]>.Failure($"Failed to process: {ex.Message}");
```

---

## 🎨 Pattern Matching Patterns

### Null/Empty Check
```csharp
value switch
{
    null or "" => Failure("Required"),
    _ => Success(value)
}
```

### Length Check
```csharp
value switch
{
    { Length: < 2 } => Failure("Too short"),
    { Length: > 100 } => Failure("Too long"),
    _ => Success(value)
}
```

### Range Check
```csharp
quantity switch
{
    < 1 => Failure("Must be at least 1"),
    > 50 => Failure("Cannot exceed 50"),
    _ => Success(quantity)
}
```

### Enum Validation
```csharp
Enum.IsDefined(value) switch
{
    false => Failure("Invalid value"),
    true => Success(value)
}
```

---

## 🚀 Endpoint Handling

```csharp
private static async Task<Results<Created<Response>, ValidationProblem>> 
    CreateAsync(CreateRequest request, DbContext db)
{
    // 1. Data Annotations validated automatically

    // 2. Domain validation
    var result = Entity.Create(request.Name, request.Quantity);

    // 3. Handle failure
    if (result.IsFailure)
    {
        return TypedResults.ValidationProblem(result.ValidationErrors);
    }

    // 4. Success path
    var entity = result.Value;
    db.Add(entity);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/api/entity/{entity.Id}", response);
}
```

---

## ⚡ Quick Tips

1. **Always use ValidationFailure for validation errors**
   - Provides structured field-level errors
   - Maps directly to HTTP 400 ValidationProblem

2. **Use Failure for infrastructure errors**
   - Database failures
   - External service failures
   - Unexpected exceptions

3. **Don't throw exceptions for business rules**
   - ❌ `throw new ArgumentException("Invalid")`
   - ✅ `return Result<T>.ValidationFailure(...)`

4. **Keep Data Annotations on DTOs**
   - .NET 10 provides automatic validation
   - Works with OpenAPI/Swagger
   - Zero configuration

5. **Use pattern matching for readability**
   - Cleaner than if/else chains
   - Exhaustiveness checking
   - Modern C# 14 syntax

---

## 📦 Common Validation Scenarios

### Email Validation
```csharp
email switch
{
    null or "" => ValidationFailure(...),
    _ when !email.Contains('@') => ValidationFailure(...),
    _ => Success(email)
}
```

### Phone Number
```csharp
phone switch
{
    null or "" => ValidationFailure(...),
    { Length: < 10 } => ValidationFailure(...),
    _ when !phone.All(char.IsDigit) => ValidationFailure(...),
    _ => Success(phone)
}
```

### URL Validation
```csharp
if (!Uri.TryCreate(url, UriKind.Absolute, out _))
{
    return ValidationFailure(...);
}
```

### GUID Validation
```csharp
guid switch
{
    _ when guid == Guid.Empty => ValidationFailure(...),
    _ => Success(guid)
}
```

---

**For detailed explanations, see:** [VALIDATION-AND-ERROR-HANDLING.md](./VALIDATION-AND-ERROR-HANDLING.md)
