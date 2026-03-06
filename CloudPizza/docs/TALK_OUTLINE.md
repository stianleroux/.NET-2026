# Conference Talk Outline

## Title Options
- "Modern .NET 10: Building Production-Ready APIs with Aspire"
- "Real-time with .NET: PostgreSQL LISTEN/NOTIFY + Server-Sent Events"
- "CloudPizza: A Tour of .NET 10's Greatest Hits"

## 45-Minute Talk Structure

### Act 1: Introduction (5 min)
**Hook:** "Who here has used polling to check for database changes?" [show of hands]
"What if I told you PostgreSQL can *tell* you when data changes, and .NET 10 makes it beautiful?"

**Topics:**
- Introduce CloudPizza demo
- Quick architecture overview
- What we'll build/show

**Demo:** Open Aspire dashboard, show running services

### Act 2: Modern API Development (10 min)
**Topics:**
- Minimal APIs vs Controllers
- Route groups and organization
- Typed results (Results<>, TypedResults)
- OpenAPI 3.1 with Scalar (not Swagger!)
- Built-in validation

**Demo:**
```csharp
// Show Order endpoint code
orders.MapPost("/", CreateOrderAsync)
    .WithName("CreateOrder")
    .Produces<CreateOrderResponse>(201)
    .ProducesValidationProblem();

// Open Scalar documentation
// Try creating an order via Scalar UI
```

**Key Points:**
- No controllers needed
- Type-safe responses
- Self-documenting
- Beautiful API docs

### Act 3: Clean Code & Modern C# (8 min)
**Topics:**
- Primary constructors for DI
- File-scoped namespaces
- Required members
- Strongly-typed IDs (no more primitive obsession!)
- Result pattern (no exception-driven flow)
- Rich domain models (not anemic!)

**Demo:**
```csharp
// Show OrderId.cs
public readonly record struct OrderId
{
    public required Guid Value { get; init; }
}

// Show Order.cs domain model
public sealed class Order
{
    public static Order Create(...) { /* business rules */ }
}

// Show Result pattern
public Result<Order> CreateOrder(...)
{
    if (invalid) return Result<Order>.Failure("reason");
    return Result<Order>.Success(order);
}
```

**Key Points:**
- Type safety everywhere
- Compile-time guarantees
- Self-documenting code

### Act 4: Real-time Magic (12 min)
**Topics:**
- The problem with polling
- PostgreSQL LISTEN/NOTIFY architecture
- Background services in .NET
- Server-Sent Events (SSE)
- Channels for async communication
- Async streams (IAsyncEnumerable)

**Architecture Diagram:**
```
INSERT → Trigger → NOTIFY → .NET Listener → Channel → SSE → Browser
```

**Demo:**
1. Show PostgreSQL trigger function in pgAdmin
2. Show PostgresNotificationService.cs code
3. Show SSE endpoint
4. Open Live Orders page
5. Display QR code on screen
6. Ask audience to scan and order
7. Watch orders appear in real-time!
8. F12 Network tab: show SSE connection

**Key Points:**
- No polling needed
- Database tells us about changes
- Scales better than SignalR for one-way updates
- Works through proxies/firewalls

### Act 5: Cloud & Observability (5 min)
**Topics:**
- .NET Aspire overview
- No Dockerfile needed!
- Built-in telemetry (OpenTelemetry)
- Service discovery
- Local dev === production
- Cloudflare Tunnel for instant public access

**Demo:**
1. Show Aspire dashboard
   - Traces
   - Metrics
   - Logs
   - Console output
2. Show docker containers running (just Postgres!)
3. Show Cloudflare tunnel running

**Key Points:**
- Modern orchestration
- Observability out of the box
- Simplified deployment

### Act 6: Recap & Q&A (5 min)
**Recap:**
- ✅ Modern Minimal APIs
- ✅ Clean Architecture & SOLID
- ✅ Latest C# features
- ✅ Real-time with LISTEN/NOTIFY + SSE
- ✅ Aspire orchestration
- ✅ Production-ready patterns

**Call to Action:**
- "All code on GitHub: [your-url]"
- "Try .NET 10 today"
- "Questions?"

## Key Takeaways for Audience

1. **Minimal APIs are production-ready** - simpler and more performant than controllers
2. **PostgreSQL LISTEN/NOTIFY beats polling** - database-level change detection is powerful
3. **Result Pattern > Exceptions** - explicit error handling is better
4. **Strong types prevent bugs** - strongly-typed IDs, typed results
5. **Aspire changes the game** - no Dockerfile, built-in observability, great DX

## Audience Engagement Ideas

### During the Talk
- **Live Polling**: "How many use SignalR? How many have tried SSE?"
- **Code Reviews**: Ask audience to spot the problem in "bad" examples
- **Predictions**: "How fast do you think this scales? 10 concurrent? 100? 1000?"

### Q&A Preparation
**Expected questions:**
- Q: "Why SSE instead of SignalR/WebSockets?"
  - A: "For one-way updates, SSE is simpler, works everywhere, uses standard HTTP. SignalR when you need bidirectional."

- Q: "What about EF Core performance?"
  - A: "EF Core 10 is fast. For extreme scale, use Dapper. For 95% of apps, EF is perfect."

- Q: "Is the Result pattern worth the boilerplate?"
  - A: "Yes! Compile-time error handling catches bugs early. Libraries like ErrorOr make it even easier."

- Q: "Does Aspire work with Kubernetes?"
  - A: "Yes! Aspire generates deployment manifests for k8s."

## Demo Failure Contingencies

### If SSE doesn't connect:
"While this reconnects, let me show you the code..." [switch to IDE]

### If database is slow:
Have pre-populated orders ready to show

### If live demo fails completely:
Have recorded video backup of the real-time ordering

## Materials Checklist

- [ ] Slides with architecture diagrams
- [ ] GitHub repo URL prominent
- [ ] QR code for repo on every slide
- [ ] CloudPizza running locally
- [ ] Cloudflare tunnel started
- [ ] QR code generated and visible
- [ ] pgAdmin open with trigger function visible
- [ ] VS Code open with key files
- [ ] Aspire dashboard open
- [ ] Browser with Live Orders page
- [ ] Backup recorded demo video
- [ ] Business cards / contact info

## Post-Talk

- [ ] Upload slides to SpeakerDeck/SlideShare
- [ ] Blog post: "What I Learned Speaking About .NET 10"
- [ ] Tweet highlights with #dotnet
- [ ] Thank attendees on social media
- [ ] Answer questions in conference Discord/Slack
