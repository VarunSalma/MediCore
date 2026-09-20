# MediCore

**A lightweight CQRS mediator library for .NET 8 with first-class interceptors, service discovery, transaction support, and strictly segregated interfaces.**

MediCore helps you structure application logic around **Commands, Queries, and Notifications** while keeping business logic independent from controllers, infrastructure, and other application entry points.

It provides a small, focused abstraction for dispatching requests, executing cross-cutting concerns through interceptors, publishing notifications, and managing command transactions.

```bash
dotnet add package MediCore
```

---

## Why MediCore?

As .NET applications grow, application logic can easily become tightly coupled to controllers, services, infrastructure code, validation, logging, and transaction handling.

MediCore provides a central request-dispatching model:

```text
Caller
   │
   ▼
 ISender
   │
   ▼
Mediator
   │
   ▼
Request Dispatcher
   │
   ▼
Interceptor Pipeline
   │
   ├── Logging
   ├── Validation
   ├── Transaction
   └── Custom Interceptors
   │
   ▼
Request Handler
```

Instead of directly depending on business services, your application can send a request and let MediCore locate and execute the appropriate handler.

```csharp
var orderId = await sender.SendAsync(
    new CreateOrder("Acme")
);
```

This keeps application entry points small and keeps business operations isolated.

---

# Key Features

## CQRS-first request model

MediCore provides dedicated abstractions for Commands and Queries.

```csharp
public sealed record CreateOrder(string Customer)
    : ICommand<Guid>;

public sealed record GetOrder(Guid OrderId)
    : IQuery<OrderDto>;
```

Commands represent operations that change application state.

Queries represent operations that retrieve application data.

The separation also allows infrastructure behaviors such as transactions to target **commands only**.

---

## Strong interface segregation

Applications do not need to depend on the entire mediator abstraction.

Inject only the capability required by the component.

```csharp
public sealed class OrderController
{
    private readonly ISender _sender;

    public OrderController(ISender sender)
    {
        _sender = sender;
    }
}
```

Available abstractions:

| Interface                | Responsibility                       |
| ------------------------ | ------------------------------------ |
| `ISender`                | Dispatch commands and queries        |
| `IPublisher`             | Publish notifications                |
| `IMediator`              | Provides both sending and publishing |
| `ICommand<T>`            | Represents a write operation         |
| `IQuery<T>`              | Represents a read operation          |
| `ICommandHandler`        | Handles commands                     |
| `IQueryHandler`          | Handles queries                      |
| `IRequestInterceptor`    | Adds cross-cutting request behavior  |
| `IValidator<T>`          | Validates a request                  |
| `ITransactionManager`    | Controls transaction creation        |
| `INotificationPublisher` | Controls notification execution      |
| `IServiceRegistry`       | Exposes discovered MediCore services |

This keeps dependencies explicit and follows the **Interface Segregation Principle**.

---

# Quick Start

## 1. Register MediCore

Register handlers from your application assembly.

```csharp
services.AddMediCore(options => options
    .RegisterHandlersFromAssemblyContaining<Program>()
    .AddDefaultInterceptors()
    .UseTransactionManager<MyEfTransactionManager>());
```

`AddDefaultInterceptors()` configures:

```text
Logging
   ↓
Validation
   ↓
Transaction
   ↓
Handler
```

A custom transaction manager is optional.

---

## 2. Create a Command

```csharp
public sealed record CreateOrder(string Customer)
    : ICommand<Guid>;
```

---

## 3. Create the Handler

```csharp
public sealed class CreateOrderHandler
    : ICommandHandler<CreateOrder, Guid>
{
    public Task<Guid> HandleAsync(
        CreateOrder command,
        CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();

        // Application logic...

        return Task.FromResult(orderId);
    }
}
```

---

## 4. Add Validation

```csharp
public sealed class CreateOrderValidator
    : IValidator<CreateOrder>
{
    // Validation implementation...
}
```

Register the validator:

```csharp
services.AddScoped<
    IValidator<CreateOrder>,
    CreateOrderValidator>();
```

The validation interceptor automatically executes registered validators before the handler.

---

## 5. Send the Command

Inject `ISender`:

```csharp
public sealed class OrderService
{
    private readonly ISender _sender;

    public OrderService(ISender sender)
    {
        _sender = sender;
    }

    public Task<Guid> CreateOrderAsync(
        string customer,
        CancellationToken cancellationToken)
    {
        return _sender.SendAsync(
            new CreateOrder(customer),
            cancellationToken);
    }
}
```

The caller does not need to know which handler executes the request.

---

# Request Flow

A typical command follows this execution path:

```text
CreateOrder
     │
     ▼
ISender.SendAsync()
     │
     ▼
Mediator
     │
     ▼
RequestDispatcher
     │
     ▼
LoggingInterceptor
     │
     ▼
ValidationInterceptor
     │
     ▼
TransactionInterceptor
     │
     ▼
CreateOrderHandler
     │
     ▼
Response
```

Each layer has a single responsibility.

---

# Interceptors

Interceptors allow cross-cutting application concerns to wrap request execution without placing infrastructure logic inside handlers.

Typical interceptor use cases include:

* Logging
* Validation
* Transactions
* Caching
* Authorization
* Performance monitoring
* Auditing
* Metrics
* Request tracing

---

## Built-in Interceptors

MediCore provides default interceptors for:

### Logging

Executes around request handling and provides a central location for request execution logging.

### Validation

Runs registered `IValidator<TRequest>` implementations before executing the handler.

### Transaction

Wraps command execution in a transaction using `ITransactionManager`.

Transactions are intended for commands and do not need to wrap read-only queries.

---

# Interceptor Execution Order

Interceptors execute in **registration order**.

The first registered interceptor becomes the outermost interceptor.

```csharp
options
    .AddInterceptor(typeof(LoggingInterceptor<,>))
    .AddInterceptor(typeof(ValidationInterceptor<,>))
    .AddInterceptor(typeof(TransactionInterceptor<,>));
```

Execution:

```text
Logging
└── Validation
    └── Transaction
        └── Handler
```

Completion flows back in reverse:

```text
Handler
   ↓
Transaction
   ↓
Validation
   ↓
Logging
```

---

# Create Custom Interceptors

Custom interceptors implement:

```csharp
IRequestInterceptor<TRequest, TResponse>
```

Example caching interceptor:

```csharp
public sealed class CachingInterceptor<TRequest, TResponse>
    : IRequestInterceptor<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
{
    public async Task<TResponse> InterceptAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Check cache before execution...

        var response = await next();

        // Store response in cache...

        return response;
    }
}
```

Register it:

```csharp
options.AddInterceptor(
    typeof(CachingInterceptor<,>));
```

Because the interceptor is constrained to:

```csharp
where TRequest : IQuery<TResponse>
```

it applies only to queries.

The same mechanism allows MediCore's transaction interceptor to target commands.

---

# Short-Circuiting Requests

An interceptor controls whether the next stage executes.

Normally:

```csharp
var response = await next();
```

executes the next interceptor or request handler.

An interceptor may return a response without calling `next()`.

This makes scenarios such as caching possible:

```text
Request
   │
   ▼
Cache Interceptor
   │
   ├── Cache hit ─────► Return cached response
   │
   └── Cache miss
          │
          ▼
        Handler
```

---

# Commands and Queries

MediCore uses separate interfaces for write and read operations.

## Command

```csharp
public sealed record UpdateOrderStatus(
    Guid OrderId,
    string Status)
    : ICommand<bool>;
```

Handler:

```csharp
public sealed class UpdateOrderStatusHandler
    : ICommandHandler<UpdateOrderStatus, bool>
{
    public async Task<bool> HandleAsync(
        UpdateOrderStatus command,
        CancellationToken cancellationToken)
    {
        // Modify application state...

        return true;
    }
}
```

---

## Query

```csharp
public sealed record GetOrder(Guid OrderId)
    : IQuery<OrderDto>;
```

Handler:

```csharp
public sealed class GetOrderHandler
    : IQueryHandler<GetOrder, OrderDto>
{
    public async Task<OrderDto> HandleAsync(
        GetOrder query,
        CancellationToken cancellationToken)
    {
        // Retrieve data...

        return new OrderDto();
    }
}
```

This separation makes request intent explicit.

```text
Command → Change state
Query   → Read state
```

---

# Transaction Management

MediCore separates transaction orchestration from the underlying database technology.

Applications depend on:

```csharp
ITransactionManager
```

instead of directly coupling MediCore to EF Core, Dapper, ADO.NET, or another persistence technology.

A custom transaction manager can be registered:

```csharp
services.AddMediCore(options => options
    .RegisterHandlersFromAssemblyContaining<Program>()
    .AddDefaultInterceptors()
    .UseTransactionManager<MyEfTransactionManager>());
```

This allows applications to integrate MediCore with their preferred persistence strategy.

Examples include:

```text
ITransactionManager
       │
       ├── EF Core Transaction Manager
       ├── DbTransaction Manager
       ├── ADO.NET Transaction Manager
       └── Custom Transaction Manager
```

MediCore also provides `TransactionScopeManager` as its default transaction implementation.

---

# Notifications

Notifications allow one message to be handled by multiple independent handlers.

This is useful for application events where several actions may need to occur after something happens.

Conceptually:

```text
OrderPlaced
     │
     ▼
IPublisher
     │
     ├── UpdateAnalyticsHandler
     ├── SendNotificationHandler
     └── AuditOrderHandler
```

Publish the concrete notification instance:

```csharp
await publisher.PublishAsync(
    new OrderPlaced(orderId));
```

Handlers are resolved using the notification's compile-time type.

---

# Notification Publishing Strategies

MediCore separates notification discovery from execution strategy through:

```csharp
INotificationPublisher
```

This allows notification handlers to be executed using different strategies.

Built-in strategies include:

```text
Sequential Publishing

Handler 1
   ↓
Handler 2
   ↓
Handler 3
```

and:

```text
Parallel Publishing

        ┌── Handler 1
Event ──┼── Handler 2
        └── Handler 3
```

The publishing strategy can therefore change without modifying notification handlers.

---

# Service Registry

MediCore includes a read-only service registry containing the components discovered during application startup.

Resolve it from the service provider:

```csharp
var registry =
    provider.GetRequiredService<IServiceRegistry>();
```

Inspect registered request handlers:

```csharp
registry.RequestHandlers;
```

Inspect notification handlers:

```csharp
registry.NotificationHandlers;
```

Inspect interceptor order:

```csharp
registry.Interceptors;
```

The registry can be useful for:

* Diagnostics
* Startup verification
* Debugging handler discovery
* Architecture tests
* Development tooling
* Visualizing registered requests
* Inspecting interceptor configuration

---

# Fail-Fast Handler Discovery

A request must have one clear handler.

MediCore detects invalid registrations during startup.

For example:

```text
CreateOrder
   ├── CreateOrderHandlerA
   └── CreateOrderHandlerB
```

is considered ambiguous.

Instead of waiting until the request is executed, MediCore fails early during startup.

This prevents hidden handler conflicts from reaching production request flows.

---

# Architecture

MediCore is divided into focused internal areas.

```text
src/MediCore
│
├── Abstractions
│   └── Pure contracts with one responsibility per interface
│
├── Core
│   ├── Mediator
│   ├── RequestDispatcher
│   └── InterceptorPipeline
│
├── Interceptors
│   ├── Logging
│   ├── Validation
│   └── Transaction
│
├── Publishing
│   ├── Sequential Publisher
│   └── Parallel Publisher
│
├── Transactions
│   └── TransactionScopeManager
│
├── Registry
│   ├── IServiceRegistry
│   ├── Assembly Scanner
│   ├── Options Builder
│   └── AddMediCore(...)
│
└── Exceptions
    ├── HandlerNotFoundException
    └── ValidationException
```

Additional projects:

```text
samples/MediCore.Sample
└── Console application demonstrating MediCore

tests/MediCore.Tests
└── xUnit test suite
```

---

# Design Principles

MediCore is built around several core principles.

### Explicit request intent

Commands and queries are represented by different interfaces.

### Small abstractions

Applications can inject `ISender`, `IPublisher`, or `IMediator` depending on what they actually require.

### Infrastructure outside handlers

Validation, logging, transactions, caching, and similar concerns belong in interceptors rather than application handlers.

### Pluggable infrastructure

Transaction management and notification publishing depend on abstractions.

### Fail fast

Invalid handler registrations are discovered at startup rather than during production request execution.

### Convention-based discovery

Handlers can be discovered automatically from assemblies during startup.

---

# Design Patterns Used

MediCore intentionally applies several software design patterns.

| Pattern                     | Usage                                                                 |
| --------------------------- | --------------------------------------------------------------------- |
| **Mediator**                | Decouples request senders from request handlers                       |
| **Facade**                  | `Mediator` hides dispatching, pipeline construction, and publishing   |
| **Chain of Responsibility** | Interceptors execute as an ordered request pipeline                   |
| **Decorator**               | Each interceptor wraps the next execution stage                       |
| **Strategy**                | Notification publishing and transaction management can be replaced    |
| **Registry**                | `ServiceRegistry` stores discovered handlers and interceptors         |
| **Builder / Options**       | `MediCoreOptions` provides fluent configuration                       |
| **Adapter**                 | Request dispatchers adapt runtime requests to strongly typed handlers |
| **Template Method**         | Dispatcher infrastructure defines the request execution structure     |
| **Flyweight / Cache**       | Dispatchers are created once and reused per request type              |
| **Dependency Inversion**    | Core functionality depends on abstractions                            |
| **CQRS**                    | Commands and queries represent separate application intentions        |

---

# When Should You Use MediCore?

MediCore is useful when your application has multiple application operations such as:

```text
CreateOrder
UpdateOrder
DeleteOrder
GetOrder
SearchOrders
ApproveOrder
CancelOrder
```

and you want each operation to have its own isolated handler.

It works particularly well with architectures such as:

* Clean Architecture
* Onion Architecture
* Layered Architecture
* Modular Monoliths
* CQRS-based applications
* ASP.NET Core APIs
* Background workers
* Console applications

A typical Clean Architecture setup might look like:

```text
API
 │
 │ ISender.SendAsync()
 ▼
Application
 │
 ├── Commands
 ├── Queries
 ├── Handlers
 ├── Validators
 └── Notifications
 │
 ▼
Infrastructure
 │
 ├── Database
 ├── External APIs
 └── Transaction Manager
```

---

# When MediCore May Be Unnecessary

Not every application requires request mediation.

For a very small application with only a few operations, directly calling application services may be simpler.

MediCore becomes more useful when the application begins to require several cross-cutting concerns such as:

```text
Validation
Logging
Transactions
Caching
Authorization
Metrics
Auditing
```

Instead of implementing these repeatedly in handlers, they can be applied through the interceptor pipeline.

---

# MediCore vs Direct Service Calls

Without request mediation:

```text
Controller
   │
   ▼
OrderService
   │
   ├── Validation
   ├── Logging
   ├── Transaction
   ├── Business Logic
   └── Persistence
```

The service may gradually accumulate several responsibilities.

With MediCore:

```text
Controller
   │
   ▼
ISender
   │
   ▼
Logging
   │
   ▼
Validation
   │
   ▼
Transaction
   │
   ▼
Command Handler
   │
   ▼
Business Logic
```

Cross-cutting responsibilities remain outside the command handler.

---

# Example Application Structure

A project using MediCore may be organized like this:

```text
Application
│
├── Orders
│   │
│   ├── Commands
│   │   ├── CreateOrder
│   │   │   ├── CreateOrderCommand.cs
│   │   │   ├── CreateOrderHandler.cs
│   │   │   └── CreateOrderValidator.cs
│   │   │
│   │   └── CancelOrder
│   │
│   ├── Queries
│   │   ├── GetOrder
│   │   └── SearchOrders
│   │
│   └── Notifications
│       └── OrderPlaced
│
└── Common
    └── Interceptors
```

MediCore does not require this structure, but it works naturally with feature-based application organization.

---

# Customization

MediCore is designed so core application behavior can be extended without changing the library itself.

Possible custom interceptors include:

```text
CachingInterceptor
AuthorizationInterceptor
AuditInterceptor
MetricsInterceptor
PerformanceInterceptor
RetryInterceptor
TracingInterceptor
IdempotencyInterceptor
```

Possible infrastructure integrations include:

```text
EF Core
Dapper
ADO.NET
Custom database abstractions
Distributed tracing
Application metrics
Structured logging
```

---

# Build the Repository

Clone the repository and run:

```bash
dotnet build
```

Run tests:

```bash
dotnet test
```

Run the sample application:

```bash
dotnet run --project samples/MediCore.Sample
```

---

# Important Notes

### Publish concrete notification types

Publish the actual notification:

```csharp
await publisher.PublishAsync(
    new OrderPlaced(orderId));
```

Notification handlers are resolved using the compile-time notification type.

### Custom transactions

Replace `TransactionScopeManager` with your own implementation of:

```csharp
ITransactionManager
```

when using explicit EF Core transactions, `DbTransaction`, Dapper, ADO.NET, or another transaction mechanism.

### Interceptor ordering matters

Given:

```text
Logging
Validation
Transaction
```

Logging wraps the entire request.

Validation executes before a transaction begins.

Transaction execution stays close to the command handler.

---

# Extension Ideas

Potential future capabilities include:

* Streaming requests
* Notification interceptors
* Per-request interceptor ordering
* Interceptor ordering attributes
* Source-generator-based registration
* Extended diagnostics
* Request execution metrics
* Additional publishing strategies
* Advanced handler discovery
* Request tracing
* OpenTelemetry integration

---

# Project Goals

MediCore aims to remain:

**Lightweight**

Keep the mediator infrastructure focused and understandable.

**Explicit**

Commands, queries, notifications, handlers, and interceptors have clearly defined responsibilities.

**Extensible**

Infrastructure behavior should be replaceable through abstractions.

**CQRS-focused**

Commands and queries should remain semantically distinct.

**Architecture-friendly**

The library should work naturally with modern .NET architectural approaches without forcing an application structure.

---

# Example

```csharp
services.AddMediCore(options => options
    .RegisterHandlersFromAssemblyContaining<Program>()
    .AddDefaultInterceptors()
    .UseTransactionManager<MyEfTransactionManager>());

public sealed record CreateOrder(string Customer)
    : ICommand<Guid>;

public sealed class CreateOrderHandler
    : ICommandHandler<CreateOrder, Guid>
{
    public Task<Guid> HandleAsync(
        CreateOrder command,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Guid.NewGuid());
    }
}

public sealed class CreateOrderValidator
    : IValidator<CreateOrder>
{
    // Validation rules...
}

services.AddScoped<
    IValidator<CreateOrder>,
    CreateOrderValidator>();

var orderId = await sender.SendAsync(
    new CreateOrder("Acme"));
```

That's the core MediCore workflow:

```text
Define Request
      ↓
Create Handler
      ↓
Add Optional Validator
      ↓
Send Request
      ↓
Interceptors Execute
      ↓
Handler Executes
      ↓
Response Returned
```

---

# MediCore

**Keep request handling focused. Keep cross-cutting concerns outside your business logic. Build CQRS applications with clear boundaries.**
