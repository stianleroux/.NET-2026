# CloudPizza Documentation Index

Welcome to the CloudPizza documentation! This project demonstrates modern .NET 10 best practices with clean architecture, the Result pattern, and functional programming principles.

## 📚 Documentation

### Getting Started
- **README.md** (root) - Project overview, quick start, and key features

### Architecture & Patterns

#### Core Documentation
1. **[Validation & Error Handling Architecture](./VALIDATION-AND-ERROR-HANDLING.md)**
   - Complete guide to our validation strategy
   - When to use each validation approach
   - Result pattern explained
   - Pattern matching examples
   - FluentValidation integration
   - Best practices and anti-patterns

2. **[Validation Quick Reference](./VALIDATION-QUICK-REFERENCE.md)**
   - Decision tree for validation approaches
   - Cheat sheets for common scenarios
   - Quick examples
   - Common validation patterns

3. **[Before & After Examples](./BEFORE-AND-AFTER.md)**
   - Side-by-side code comparisons
   - Migration benefits
   - Performance improvements
   - Real-world examples

4. **[Migration Checklist](./MIGRATION-CHECKLIST.md)**
   - Completed migrations
   - Verification steps
   - Team training checklist
   - Success metrics

## 🎯 Quick Navigation

### I want to...

#### Learn the Architecture
→ Start with [VALIDATION-AND-ERROR-HANDLING.md](./VALIDATION-AND-ERROR-HANDLING.md)

#### See Code Examples
→ Check [VALIDATION-QUICK-REFERENCE.md](./VALIDATION-QUICK-REFERENCE.md)

#### Understand the Migration
→ Review [BEFORE-AND-AFTER.md](./BEFORE-AND-AFTER.md)

#### Track Progress
→ See [MIGRATION-CHECKLIST.md](./MIGRATION-CHECKLIST.md)

#### Write New Code
→ Use [VALIDATION-QUICK-REFERENCE.md](./VALIDATION-QUICK-REFERENCE.md) as cheat sheet

## 🏗️ Project Structure

```
CloudPizza/
├── src/
│   ├── CloudPizza.Api/              # REST API with Minimal APIs
│   ├── CloudPizza.Web/              # Blazor frontend
│   ├── CloudPizza.Shared/           # Common code & domain
│   ├── CloudPizza.Infrastructure/   # Data access & services
│   └── CloudPizza.AppHost/          # Aspire orchestration
│
├── docs/                            # Documentation (you are here!)
│   ├── INDEX.md                     # This file
│   ├── VALIDATION-AND-ERROR-HANDLING.md
│   ├── VALIDATION-QUICK-REFERENCE.md
│   ├── BEFORE-AND-AFTER.md
│   └── MIGRATION-CHECKLIST.md
│
└── tests/                           # Test projects
```

## 💡 Key Concepts

### The Result Pattern

Instead of throwing exceptions for validation, we return a `Result<T>` type that explicitly represents success or failure.

```csharp
// ❌ Old way
public static Order Create(string name)
{
    if (string.IsNullOrEmpty(name))
        throw new ArgumentException("Name required");
    return new Order { Name = name };
}

// ✅ New way
public static Result<Order> Create(string name)
{
    var validation = name switch
    {
        null or "" => Result<string>.ValidationFailure(...),
        _ => Result<string>.Success(name)
    };
    
    if (validation.IsFailure)
        return Result<Order>.ValidationFailure(...);
    
    return Result<Order>.Success(new Order { Name = name });
}
```

### Three Validation Approaches

| Approach | When to Use | Example |
|----------|-------------|---------|
| **Data Annotations** | API DTOs | `[Required] string Name` |
| **FluentValidation** | Value objects | `OrderIdValidator` |
| **Pattern Matching** | Domain entities | `Order.Create()` |

### Benefits

✅ **No Exceptions for Business Rules** - Better performance  
✅ **Explicit Error Handling** - Compile-time safety  
✅ **Structured Errors** - Field-level validation messages  
✅ **Type Safety** - Compiler ensures handling  
✅ **Testability** - Easy to test success and failure cases  

## 📖 Recommended Reading Order

### For New Developers
1. Read project README.md
2. Review [VALIDATION-QUICK-REFERENCE.md](./VALIDATION-QUICK-REFERENCE.md)
3. Study code in `src/CloudPizza.Shared/Domain/Order.cs`
4. Study code in `src/CloudPizza.Api/Features/Orders/OrderEndpoints.cs`
5. Deep dive: [VALIDATION-AND-ERROR-HANDLING.md](./VALIDATION-AND-ERROR-HANDLING.md)

### For Experienced Developers
1. Review [BEFORE-AND-AFTER.md](./BEFORE-AND-AFTER.md) for quick understanding
2. Skim [VALIDATION-AND-ERROR-HANDLING.md](./VALIDATION-AND-ERROR-HANDLING.md) for architecture
3. Keep [VALIDATION-QUICK-REFERENCE.md](./VALIDATION-QUICK-REFERENCE.md) open while coding
4. Reference [MIGRATION-CHECKLIST.md](./MIGRATION-CHECKLIST.md) for PR reviews

### For Architects
1. Read [VALIDATION-AND-ERROR-HANDLING.md](./VALIDATION-AND-ERROR-HANDLING.md) completely
2. Review [BEFORE-AND-AFTER.md](./BEFORE-AND-AFTER.md) for patterns
3. Check [MIGRATION-CHECKLIST.md](./MIGRATION-CHECKLIST.md) for metrics
4. Review actual code in domain and API layers

## 🔍 Code Examples Location

### Domain Layer
- `src/CloudPizza.Shared/Domain/Order.cs` - Pattern matching validation
- `src/CloudPizza.Shared/Domain/OrderId.cs` - FluentValidation + Result
- `src/CloudPizza.Shared/Common/Result.cs` - Result<T> implementation

### API Layer
- `src/CloudPizza.Api/Features/Orders/OrderEndpoints.cs` - Result handling
- `src/CloudPizza.Api/Features/QrCode/QrCodeEndpoints.cs` - Result handling
- `src/CloudPizza.Shared/Contracts/OrderContracts.cs` - Data Annotations

### Infrastructure Layer
- `src/CloudPizza.Infrastructure/Services/QrCodeService.cs` - Service with Result

## 🎓 Learning Resources

### Internal
- Code comments in Result.cs
- XML documentation on all public methods
- Unit tests showing usage patterns

### External
- [Result Pattern explained](https://enterprisecraftsmanship.com/posts/functional-c-handling-failures-input-errors/)
- [C# 14 Pattern Matching](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/patterns)
- [FluentValidation docs](https://docs.fluentvalidation.net/)
- [Minimal API validation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)

## 🤝 Contributing

When adding new features:
1. Follow the validation strategy documented here
2. Use Result<T> for domain operations
3. Keep Data Annotations on API DTOs
4. Add examples to documentation if introducing new patterns
5. Update MIGRATION-CHECKLIST.md if applicable

## 📝 Documentation Standards

### When to Update Documentation

- ✅ Adding a new validation pattern
- ✅ Changing error handling approach
- ✅ Adding new validation rules
- ✅ Migrating code to Result pattern
- ✅ Discovering common mistakes or edge cases

### How to Update Documentation

1. Update the relevant doc file
2. Add examples if introducing new patterns
3. Update the migration checklist if needed
4. Update this INDEX.md if adding new docs

## 🎯 Quick Reference Cards

### Result<T> Creation

```csharp
// Success
Result<Order>.Success(order)

// Validation failure (business rule)
Result<Order>.ValidationFailure(
    "Validation failed",
    new Dictionary<string, string[]> { ["Field"] = ["Error"] })

// Infrastructure failure
Result<Order>.Failure("Database connection failed")
```

### Pattern Matching

```csharp
value switch
{
    null or "" => ValidationFailure(...),
    { Length: < 2 } => ValidationFailure(...),
    _ => Success(value)
}
```

### Endpoint Handling

```csharp
var result = Entity.Create(...);

if (result.IsFailure)
    return TypedResults.ValidationProblem(result.ValidationErrors);

var entity = result.Value;
// Success path...
```

## 📞 Getting Help

### Questions?
- Check the relevant doc file above
- Search for examples in the codebase
- Ask in #engineering-help Slack
- Review unit tests for usage patterns

### Found an Issue?
- Check if it's already documented
- Create a GitHub issue with:
  - What you tried
  - What happened
  - What you expected
  - Relevant code examples

### Want to Improve Docs?
- PRs welcome!
- Follow existing format
- Add examples for clarity
- Keep it concise but complete

---

## 📊 Documentation Status

| Document | Status | Last Updated |
|----------|--------|--------------|
| INDEX.md | ✅ Complete | March 2026 |
| VALIDATION-AND-ERROR-HANDLING.md | ✅ Complete | March 2026 |
| VALIDATION-QUICK-REFERENCE.md | ✅ Complete | March 2026 |
| BEFORE-AND-AFTER.md | ✅ Complete | March 2026 |
| MIGRATION-CHECKLIST.md | ✅ Complete | March 2026 |

---

**Welcome to CloudPizza! Happy coding! 🍕**
