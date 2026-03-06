# AI Prompts for .NET 10 Development with SOLID & Clean Code

This document contains AI prompts you can use with GitHub Copilot, ChatGPT, or other AI assistants to help you write clean, SOLID-compliant .NET 10 code.

## Table of Contents
- [General Code Review Prompts](#general-code-review-prompts)
- [SOLID Principle Prompts](#solid-principle-prompts)
- [Clean Architecture Prompts](#clean-architecture-prompts)
- [Refactoring Prompts](#refactoring-prompts)
- [Testing Prompts](#testing-prompts)

---

## General Code Review Prompts

### Review My Code for Best Practices
```
Review this C# code for .NET 10 best practices. Check for:
- SOLID principle violations
- Missing null checks
- Async/await issues
- Missing CancellationToken parameters
- Opportunities to use modern C# features (primary constructors, required members, records)
- Performance issues
- Security concerns

[Paste your code here]
```

### Suggest Modern .NET 10 Improvements
```
I have this .NET code. Suggest improvements using .NET 10 features:
- Primary constructors
- File-scoped namespaces
- Required members
- Init-only setters
- Records
- Pattern matching
- Minimal APIs (if applicable)

[Paste your code here]
```

---

## SOLID Principle Prompts

### Single Responsibility Principle (SRP)
```
Does this class follow the Single Responsibility Principle? 
If not, suggest how to split it into focused classes.

[Paste your class here]
```

### Open/Closed Principle (OCP)
```
How can I make this code more extensible without modifying existing code?
Suggest patterns like Strategy, Template Method, or interfaces.

[Paste your code here]
```

### Liskov Substitution Principle (LSP)
```
Review this inheritance hierarchy for LSP violations.
Ensure derived classes can substitute base classes without breaking behavior.

[Paste your classes here]
```

### Interface Segregation Principle (ISP)
```
Is this interface too fat? Should it be split into smaller, more focused interfaces?
Suggest how to segregate it properly.

[Paste your interface here]
```

### Dependency Inversion Principle (DIP)
```
Review this code for DIP violations.
- Are there concrete dependencies that should be abstractions?
- Should I create interfaces?
- How can I improve testability through DI?

[Paste your code here]
```

---

## Clean Architecture Prompts

### Create a Domain Entity
```
Create a rich domain entity for [EntityName] following Clean Architecture and DDD principles:
- No anemic model (include behavior)
- Private setters
- Factory methods for creation
- Business rule validation
- Use strongly-typed IDs
- Use .NET 10 features (primary constructors, required members, records where appropriate)
- Add XML documentation comments

Business rules:
[Describe your business rules here]
```

### Create a Repository Interface
```
Create a repository interface for [EntityName] following Clean Architecture:
- Use strongly-typed IDs
- Return Task/ValueTask for async operations
- Include CancellationToken parameters
- Keep interface small and focused (ISP)
- Use Result<T> pattern for operations that can fail

Required operations:
[List operations: GetByIdAsync, SaveAsync, etc.]
```

### Implement Result Pattern
```
Create a generic Result<T> type for explicit error handling:
- Success/Failure states
- Error messages
- Validation errors support
- Fluent API (Map, Bind methods)
- Use modern C# features

Then show an example of using it in a service method.
```

### Create a Minimal API Endpoint
```
Create a Minimal API endpoint for [operation]:
- Use route groups
- Use typed results (Results<Ok<T>, NotFound, ValidationProblem>)
- Include proper validation
- Add OpenAPI metadata (WithName, WithSummary, WithDescription)
- Use dependency injection properly
- Include CancellationToken
- Use Result pattern for business logic errors

Endpoint details:
- HTTP method: [GET/POST/PUT/DELETE]
- Route: [/api/...]
- Purpose: [describe what it does]
```

---

## Refactoring Prompts

### Eliminate Code Smells
```
Identify and fix code smells in this code:
- Long methods (>20 lines)
- Deep nesting (>3 levels)
- Magic numbers/strings
- Primitive obsession
- Feature envy
- Duplicate code

[Paste your code here]
```

### Convert to Primary Constructors
```
Refactor this class to use .NET 10 primary constructors:

[Paste your class with traditional constructors]
```

### Convert to Strongly-typed IDs
```
Refactor this code to use strongly-typed IDs instead of primitives:

[Paste your code using Guid, int, string as IDs]
```

### Introduce Result Pattern
```
Refactor this code to use Result<T> pattern instead of throwing exceptions for business logic:

[Paste your code that throws exceptions]
```

### Convert to Async Streams
```
Refactor this code to use IAsyncEnumerable<T> for streaming data:

[Paste your code returning IEnumerable<T> or List<T>]
```

---

## Testing Prompts

### Generate Unit Tests
```
Generate unit tests for this class using xUnit:
- Test all public methods
- Use descriptive test method names (Given_When_Then)
- Test both success and failure paths
- Mock dependencies using Moq
- Use test data builders
- Test edge cases

[Paste your class here]
```

### Create Test Data Builders
```
Create a test data builder for this entity using the Builder pattern:
- Fluent API
- Sensible defaults
- Methods to override specific properties
- Build() method to create the entity

[Paste your entity here]
```

### Generate Integration Tests
```
Generate integration tests for this API endpoint:
- Use WebApplicationFactory
- Test HTTP status codes
- Test response body
- Test validation errors
- Test authorization (if applicable)

[Paste your endpoint code here]
```

---

## Feature-Specific Prompts

### Create a Background Service
```
Create a background service that [describe purpose]:
- Implement IHostedService or BackgroundService
- Use primary constructor for DI
- Include proper cancellation handling
- Use ILogger for logging
- Handle exceptions gracefully
- Include retry logic if needed
```

### Implement Server-Sent Events (SSE)
```
Create an SSE endpoint that streams [data type]:
- Use IAsyncEnumerable<T> for streaming
- Proper SSE formatting (event: data: id:)
- Handle client disconnection
- Use Channels for message distribution
- Include proper error handling
```

### Create EF Core DbContext
```
Create an EF Core DbContext for these entities:
- Use primary constructor
- Configure entities using Fluent API
- Use strongly-typed IDs with value conversions
- Include indexes for performance
- Add query filters if needed
- Use file-scoped namespaces

Entities:
[List your entities]
```

---

## Code Review Checklist Prompt

### Comprehensive Code Review
```
Perform a comprehensive code review of this .NET 10 code:

**SOLID Principles:**
- [ ] Single Responsibility (one reason to change)
- [ ] Open/Closed (open for extension, closed for modification)
- [ ] Liskov Substitution (derived classes substitutable)
- [ ] Interface Segregation (focused interfaces)
- [ ] Dependency Inversion (depend on abstractions)

**Modern C# Features:**
- [ ] Primary constructors used where appropriate
- [ ] File-scoped namespaces
- [ ] Required members for initialization
- [ ] Init-only setters for immutability
- [ ] Records for DTOs/value objects
- [ ] Pattern matching where applicable

**Best Practices:**
- [ ] Async all the way down
- [ ] CancellationToken parameters
- [ ] No magic strings/numbers
- [ ] Strongly-typed IDs (no primitive obsession)
- [ ] Result pattern for business errors
- [ ] Rich domain models (not anemic)
- [ ] Proper null handling
- [ ] XML documentation comments

**Architecture:**
- [ ] Separation of concerns
- [ ] Dependency injection used properly
- [ ] No service locator anti-pattern
- [ ] Testable code
- [ ] Feature-based organization

**Performance:**
- [ ] Async/await used correctly
- [ ] No blocking calls
- [ ] Proper use of ValueTask
- [ ] EF Core queries are efficient
- [ ] No N+1 query problems

[Paste your code here]
```

---

## Example Usage

### Before (Bad Code)
```csharp
public class OrderService
{
    private OrderRepository _repo;
    
    public OrderService()
    {
        _repo = new OrderRepository();
    }
    
    public void ProcessOrder(Guid orderId)
    {
        var order = _repo.GetOrder(orderId);
        if (order == null)
            throw new Exception("Order not found");
            
        order.Status = "Processing";
        _repo.Save(order);
    }
}
```

### Prompt
```
Refactor this code to follow SOLID principles and use modern .NET 10 features:
- Use dependency injection
- Use strongly-typed IDs
- Use Result pattern instead of exceptions
- Make it async
- Use primary constructors
- Add proper error handling

[Paste bad code above]
```

### After (Good Code)
```csharp
public sealed class OrderService(IOrderRepository repository, ILogger<OrderService> logger)
{
    public async Task<Result> ProcessOrderAsync(OrderId orderId, CancellationToken ct)
    {
        var orderResult = await repository.GetByIdAsync(orderId, ct);
        
        if (orderResult.IsFailure)
            return Result.Failure($"Order {orderId} not found");
        
        var order = orderResult.Value;
        order.Process(); // Business logic in domain
        
        await repository.SaveAsync(order, ct);
        logger.LogInformation("Order {OrderId} processed", orderId);
        
        return Result.Success();
    }
}
```

---

## Quick Reference Prompts

### Quick SOLID Check
```
Quick SOLID check: Does this code violate any SOLID principles? List violations.
[Paste code]
```

### Quick Modernization
```
How can I modernize this code using .NET 10 features?
[Paste code]
```

### Quick Security Review
```
Security review: Are there any security issues in this code?
[Paste code]
```

### Quick Performance Check
```
Performance review: Are there any performance issues or anti-patterns?
[Paste code]
```

---

## Tips for Using These Prompts

1. **Be Specific**: The more context you provide, the better the AI response
2. **Iterate**: Use follow-up prompts to refine the suggestions
3. **Learn**: Don't just copy-paste; understand *why* the changes are improvements
4. **Validate**: Always review AI-generated code for correctness
5. **Combine**: Use multiple prompts for comprehensive reviews

## Integration with GitHub Copilot

In your editor, use these as comments to trigger Copilot suggestions:

```csharp
// TODO: Refactor to use primary constructor and DI
// TODO: Convert to strongly-typed OrderId
// TODO: Use Result pattern instead of throwing exceptions
// TODO: Add async support with CancellationToken
// TODO: Apply Single Responsibility Principle
```

---

**Remember**: These prompts are tools to help you write better code. Always understand the changes before applying them!
