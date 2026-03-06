# Architecture Decision Records

## ADR-001: Use Result Pattern Instead of Exceptions for Business Logic

**Date**: 2026-03-05  
**Status**: Accepted

### Context
We need a consistent way to handle business logic errors that doesn't rely on exceptions for control flow.

### Decision
Implement a Result<T> type for explicit error handling in business logic. Reserve exceptions for infrastructure failures.

### Consequences
**Positive:**
- Explicit error handling in method signatures
- Better performance (no exception throwing)
- Forces error handling at compile time
- Clear separation between business and infrastructure errors

**Negative:**
- More boilerplate code
- Learning curve for developers used to exceptions

## ADR-002: Use PostgreSQL LISTEN/NOTIFY for Real-time Updates

**Date**: 2026-03-05  
**Status**: Accepted

### Context
Need efficient real-time notifications when orders are created, without polling the database.

### Decision
Use PostgreSQL's built-in LISTEN/NOTIFY mechanism with database triggers, combined with Server-Sent Events (SSE) to push updates to clients.

### Consequences
**Positive:**
- Database-level change detection
- No polling overhead
- Instant notifications
- Scales well with proper infrastructure

**Negative:**
- Requires persistent connection to database
- More complex than simple polling
- SSE requires careful connection management

## ADR-003: Use Strongly-typed IDs Throughout

**Date**: 2026-03-05  
**Status**: Accepted

### Context
Using raw GUIDs everywhere leads to bugs where IDs are accidentally mixed up.

### Decision
Create typed ID wrappers (readonly record structs) for all entity IDs.

### Consequences
**Positive:**
- Compile-time type safety
- Prevents mixing different ID types
- Self-documenting code

**Negative:**
- Slightly more code
- Requires conversion in some places

## ADR-004: Feature-based Folder Organization

**Date**: 2026-03-05  
**Status**: Accepted

### Context
Traditional layer-based organization spreads feature code across many folders.

### Decision
Organize code by feature (vertical slices) rather than technical layers.

### Consequences
**Positive:**
- Related code stays together
- Easier to find and modify features
- Supports parallel development

**Negative:**
- May duplicate some infrastructure code
- Requires discipline to maintain boundaries
