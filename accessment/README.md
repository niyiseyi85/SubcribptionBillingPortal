# Subscription Billing Portal

A production-grade **Subscription Billing System** built with **.NET 10**, demonstrating Domain-Driven Design (DDD), Clean Architecture, CQRS, the Outbox Pattern, and idempotent command handling.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 / C# 14 |
| Web API | ASP.NET Core Minimal APIs |
| CQRS / Mediator | MediatR 14 |
| Validation | FluentValidation 12 |
| Object Mapping | Mapster 10 |
| Persistence | EF Core 10 (InMemory) |
| Background Jobs | Quartz.NET |
| Logging | Serilog |
| Testing | xUnit + FluentAssertions + Moq |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git

### Run locally

```bash
git clone https://github.com/niyiseyi85/SubcribptionBillingPortal.git
cd SubcribptionBillingPortal

dotnet restore
dotnet build
dotnet run --project src/SubscriptionBillingPortal.API
```

Swagger UI is available at:
```
http://localhost:<port>/swagger
```

### Run tests

```bash
dotnet test
```

---

## Project Structure

```
src/
├── SubscriptionBillingPortal.Domain          # Aggregates, Entities, Value Objects, Events
├── SubscriptionBillingPortal.Application     # CQRS Commands/Queries, DTOs, Validators
├── SubscriptionBillingPortal.Infrastructure  # EF Core, Repositories, Background Jobs
├── SubscriptionBillingPortal.API             # Minimal API Endpoints, Middleware
└── SubscriptionBillingPortal.Shared          # Pagination, ApiResponse wrapper

tests/
├── SubscriptionBillingPortal.Domain.Tests
└── SubscriptionBillingPortal.Application.Tests
```

---

## API Endpoints

### Customers
| Method | Route | Description |
|---|---|---|
| `POST` | `/customers` | Create a new customer |

### Subscriptions
| Method | Route | Description |
|---|---|---|
| `POST` | `/subscriptions` | Create a subscription |
| `POST` | `/subscriptions/{id}/activate` | Activate a subscription |
| `POST` | `/subscriptions/{id}/cancel` | Cancel a subscription |

### Invoices
| Method | Route | Description |
|---|---|---|
| `GET` | `/invoices/{subscriptionId}` | Get paginated invoices for a subscription |
| `POST` | `/invoices/{id}/pay` | Pay an invoice |

### Idempotency
All mutating endpoints accept an `Idempotency-Key` header (UUID). Re-submitting the same key is a safe no-op.

---

## Subscription Plans

| Plan | Monthly | Quarterly | Annual |
|---|---|---|---|
| Basic | $9.99 | $26.99 | $99.99 |
| Pro | $29.99 | $79.99 | $299.99 |
| Enterprise | $99.99 | $269.99 | $999.99 |

Pricing is a domain business rule encapsulated in the `SubscriptionPlan` value object — no external service required.

---

## Design Decisions

### Domain Model

**`Invoice` is an Entity inside the `Subscription` aggregate, not a separate aggregate root.**  
Invoices have no identity or lifecycle independent of their subscription. Accessing an invoice always goes through `Subscription` — this enforces the aggregate boundary and prevents inconsistent state.

**`SubscriptionPlan` is a `sealed class` Value Object (not a `record`).**  
EF Core owned entities require a private parameterless constructor. C# `record` types cannot satisfy this without workarounds, so a sealed class was chosen. The `sealed` modifier prevents inheritance, preserving value-object semantics.

**Pricing lives in the domain.**  
The `SubscriptionPlan` value object contains the full pricing table as a private static `Dictionary`. This makes prices a domain invariant — they cannot be manipulated by the application or infrastructure layers.

**`decimal` for money, not a `Money` value object.**  
For this scope (single currency, USD only) adding a `Money` type would be over-engineering. The decision is explicit and deliberate. A multi-currency extension would introduce a `Money` value object at that point.

### Outbox Pattern
Domain events are never dispatched in-process during the request. Instead, `UnitOfWork.SaveChangesAsync` serialises all pending domain events into an `OutboxMessage` table atomically with the business data. `OutboxProcessorJob` (Quartz, every 10 s) deserialises and dispatches them via MediatR. This guarantees at-least-once delivery and full auditability.

### Idempotent Commands
Every command carries an `IdempotencyKey` (Guid). Before processing, the handler checks `IIdempotencyService`. If the key has been seen before, the command is short-circuited. This makes all write endpoints safe to retry (network timeout, client-side retry logic, etc.).

### Billing Cycle
`InvoiceGenerationJob` runs on a schedule (Quartz). It queries all `Active` subscriptions whose `NextBillingDate ≤ UtcNow` and calls `GenerateInvoice()` on the aggregate. The new invoice and updated `NextBillingDate` are persisted atomically. `BillingIntervalDays` is driven by the `SubscriptionPlan` value object — Basic/Pro/Enterprise × Monthly/Quarterly/Annual each produce the correct interval automatically.

### Clean Architecture Boundaries
- **Domain** has zero dependencies on any other layer.  
- **Application** depends only on Domain and defines repository/service interfaces.  
- **Infrastructure** implements those interfaces; EF Core and Quartz live here.  
- **API** depends only on Application (via MediatR). It never references Domain types directly.

---

## Running the Assessment Scenario (Quick Demo)

```bash
# 1. Create a customer
POST /customers
{ "firstName": "Jane", "lastName": "Doe", "email": "jane@example.com" }

# 2. Create a Pro/Monthly subscription
POST /subscriptions
{ "customerId": "<id>", "planType": "Pro", "billingInterval": "Monthly" }

# 3. Activate (generates first invoice automatically)
POST /subscriptions/<id>/activate

# 4. Pay the invoice
POST /invoices/<invoiceId>/pay
{ "subscriptionId": "<id>", "paymentReference": "PAY-001" }

# 5. View invoices (paginated)
GET /invoices/<subscriptionId>?pageNumber=1&pageSize=20

# 6. Cancel
POST /subscriptions/<id>/cancel
```
