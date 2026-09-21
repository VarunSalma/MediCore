# MediCore

[![NuGet](https://img.shields.io/nuget/v/MediCore.svg)](https://www.nuget.org/packages/MediCore)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)

**A lightweight CQRS mediator for .NET 8 with built-in request interceptors (logging, validation, transactions), notifications, and a startup service registry.**

MediCore lets you structure application logic around **Commands, Queries, and Notifications**, keeping business logic independent of controllers and infrastructure, and keeping cross-cutting concerns (logging, validation, transactions) out of your handlers entirely.

```bash
dotnet add package MediCore
```

- **NuGet:** https://www.nuget.org/packages/MediCore
- **Docs / landing page:** https://varunsalma.github.io/MediCore/
- **License:** MIT — see [`LICENSE.txt`](LICENSE.txt)
- **Target framework:** .NET 8.0
- **Author:** Varun Kumar A

---

## Why MediCore?

As a .NET application grows, its logic can become tightly coupled to controllers, infrastructure, validation, logging, and transaction handling. MediCore introduces a central dispatching model instead:

```text
Caller → ISender → Mediator → RequestDispatcher → Interceptor Pipeline → Handler
                                                        │
                                                        ├── Logging
                                                        ├── Validation
                                                        ├── Transaction
                                                        └── Custom interceptors
```

Instead of depending directly on a business service, callers send a request and MediCore locates and executes the right handler:

```csharp
var orderId = await sender.SendAsync(new CreateOrder("Acme"));
```

---

## Key Features

| Feature | Description |
|---|---|
| **CQRS-first requests** | `ICommand<T>` / `IQuery<T>` make write vs. read intent explicit at compile time |
| **Interface segregation** | Inject `ISender`, `IPublisher`, or `IMediator` — only what a component actually needs |
| **Interceptor pipeline** | A Chain-of-Responsibility pipeline wraps every request; interceptors can short-circuit |
| **Built-in interceptors** | Logging, validation (`IValidator<T>`), and transaction handling ship out of the box |
| **Notifications** | One event (`INotification`), many independent handlers, sequential or parallel publishing |
| **Pluggable transactions** | `ITransactionManager` abstracts EF Core / Dapper / ADO.NET / `TransactionScope` |
| **Service registry** | `IServiceRegistry` exposes every handler and interceptor discovered at startup |
| **Fail-fast discovery** | Duplicate or missing handlers are detected at startup, not at request time |

### Available abstractions

| Interface | Responsibility |
|---|---|
| `ISender` | Dispatch commands and queries |
| `IPublisher` | Publish notifications |
| `IMediator` | Both sending and publishing (`ISender` + `IPublisher`) |
| `ICommand<T>` / `ICommand` | A write request (returns `T`, or nothing) |
| `IQuery<T>` | A read request |
| `IRequestHandler<TRequest, TResponse>` | Handles a request |
| `ICommandHandler<T, TResponse>` / `IQueryHandler<T, TResponse>` | Semantic aliases of `IRequestHandler<,>` for commands/queries |
| `INotification` / `INotificationHandler<T>` | An event and its handler(s) |
| `IRequestInterceptor<TRequest, TResponse>` | Cross-cutting behavior around a request |
| `IValidator<T>` | Validates a request before it reaches the handler |
| `ITransactionManager` | Begins/commits/rolls back a transaction |
| `INotificationPublisher` | Strategy for invoking notification handlers |
| `IServiceRegistry` | Read-only catalogue of everything discovered at startup |

---

## Quick Start

### 1. Register MediCore

```csharp
services.AddMediCore(options => options
    .RegisterHandlersFromAssemblyContaining<Program>()
    .AddDefaultInterceptors()
    .UseTransactionManager<MyEfTransactionManager>()); // optional — defaults to TransactionScopeManager
```

`AddDefaultInterceptors()` wires up, in order: **Logging → Validation → Transaction → Handler**.

### 2. Define a command

```csharp
public sealed record CreateOrder(string Customer) : ICommand<Guid>;
```

### 3. Implement the handler

```csharp
public sealed class CreateOrderHandler : ICommandHandler<CreateOrder, Guid>
{
    public Task<Guid> HandleAsync(CreateOrder command, CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();
        // application logic...
        return Task.FromResult(orderId);
    }
}
```

### 4. (Optional) Add a validator

```csharp
public sealed class CreateOrderValidator : IValidator<CreateOrder>
{
    public Task<ValidationResult> ValidateAsync(CreateOrder instance, CancellationToken cancellationToken)
        => Task.FromResult(string.IsNullOrWhiteSpace(instance.Customer)
            ? ValidationResult.Failure(new ValidationError(nameof(instance.Customer), "Customer is required."))
            : ValidationResult.Success);
}
```

```csharp
services.AddScoped<IValidator<CreateOrder>, CreateOrderValidator>();
```

The validation interceptor runs every registered `IValidator<T>` before the handler executes and throws `ValidationException` if any of them fail.

### 5. Send the command

```csharp
public sealed class OrderService(ISender sender)
{
    public Task<Guid> CreateOrderAsync(string customer, CancellationToken ct)
        => sender.SendAsync(new CreateOrder(customer), ct);
}
```

The caller never needs to know which class handles the request.

---

## Commands vs. Queries

```csharp
public sealed record UpdateOrderStatus(Guid OrderId, string Status) : ICommand<bool>;
public sealed record GetOrder(Guid OrderId) : IQuery<OrderDto>;
```

- **Commands** change state and are wrapped in a transaction by the default `TransactionInterceptor`.
- **Queries** only read state and are never wrapped in a transaction — the interceptor's generic constraint (`where TRequest : ICommand<TResponse>`) means it simply doesn't apply to them.

---

## Interceptors

Interceptors wrap request execution so cross-cutting concerns don't have to live inside handlers. Each one decides whether to call `next()`:

```csharp
public sealed class CachingInterceptor<TRequest, TResponse> : IRequestInterceptor<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
{
    public async Task<TResponse> InterceptAsync(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // check cache...
        var response = await next();
        // store in cache...
        return response;
    }
}
```

```csharp
options.AddInterceptor(typeof(CachingInterceptor<,>));
```

Because it's constrained to `IQuery<TResponse>`, it only ever applies to queries — the same mechanism the built-in `TransactionInterceptor` uses to target commands only.

### Built-in interceptors

| Interceptor | Behavior |
|---|---|
| `LoggingInterceptor<,>` | Logs start, duration, and failures around every request |
| `ValidationInterceptor<,>` | Runs registered `IValidator<T>` implementations, throws `ValidationException` on failure |
| `TransactionInterceptor<,>` | Wraps `ICommand<T>` execution in a transaction via `ITransactionManager`; commits on success, rolls back on exception |

### Execution order

Interceptors run in **registration order** — the first one registered is the outermost:

```csharp
options
    .AddInterceptor(typeof(LoggingInterceptor<,>))
    .AddInterceptor(typeof(ValidationInterceptor<,>))
    .AddInterceptor(typeof(TransactionInterceptor<,>));
```

```text
Logging → Validation → Transaction → Handler
```

`AddInterceptor` accepts an open generic (`typeof(MyInterceptor<,>)`, applied to every matching request) or a closed type (applied to one specific request).

---

## Notifications

One event, many independent handlers — useful when several unrelated things need to happen after something occurs:

```csharp
public sealed record OrderPlaced(Guid OrderId) : INotification;

await publisher.PublishAsync(new OrderPlaced(orderId));
```

```text
OrderPlaced → IPublisher → UpdateAnalyticsHandler
                          → SendEmailHandler
                          → AuditOrderHandler
```

Handlers are resolved by the notification's **compile-time type**, so publish the concrete type, not a variable typed as `INotification`.

### Publishing strategies (`INotificationPublisher`)

| Strategy | Behavior |
|---|---|
| `SequentialNotificationPublisher` (default) | Runs handlers one after another; a failing handler stops the rest |
| `ParallelNotificationPublisher` | Starts all handlers at once via `Task.WhenAll` |

```csharp
options.UseParallelNotificationPublisher();
// or: options.UseNotificationPublisher<MyPublisher>();
```

---

## Transactions

Applications depend on `ITransactionManager`, not directly on EF Core, Dapper, or ADO.NET:

```csharp
public interface ITransactionManager
{
    Task<ITransaction> BeginAsync(CancellationToken cancellationToken);
}
```

MediCore ships `TransactionScopeManager` as the default implementation, built on `System.Transactions.TransactionScope` (ambient transaction, `ReadCommitted`, 30s timeout, async flow enabled). Swap in your own for explicit `DbTransaction`/EF Core transactions:

```csharp
services.AddMediCore(options => options
    .RegisterHandlersFromAssemblyContaining<Program>()
    .AddDefaultInterceptors()
    .UseTransactionManager<MyEfTransactionManager>());
```

---

## Service Registry

`IServiceRegistry` is a read-only, singleton catalogue of everything MediCore discovered at startup — useful for diagnostics, health checks, or startup assertions:

```csharp
var registry = provider.GetRequiredService<IServiceRegistry>();

registry.RequestHandlers;      // every discovered request → handler mapping
registry.NotificationHandlers; // every discovered notification → handler(s) mapping
registry.Interceptors;         // interceptors, in execution order (outermost first)
```

### Fail-fast handler discovery

A request must resolve to exactly one handler. If two handlers are found for the same request type, `AddMediCore` throws `InvalidOperationException` at startup rather than letting the ambiguity reach a live request. Sending a request with **no** registered handler throws `HandlerNotFoundException` at dispatch time.

---

## Project Structure

```text
MediCore-main/
├── src/MediCore/
│   ├── Abstractions/     ICommand, IQuery, IRequest, IRequestHandler, ISender, IPublisher,
│   │                     IMediator, INotification(Handler), IRequestInterceptor, IValidator,
│   │                     ITransactionManager, Unit
│   ├── Core/             Mediator (facade), RequestDispatcher (adapter), InterceptorPipeline
│   ├── Interceptors/     LoggingInterceptor, ValidationInterceptor, TransactionInterceptor
│   ├── Publishing/       SequentialNotificationPublisher, ParallelNotificationPublisher
│   ├── Transactions/     TransactionScopeManager (default ITransactionManager)
│   ├── Registry/         IServiceRegistry, ServiceRegistry, HandlerScanner, MediCoreOptions,
│   │                     MediCoreServiceCollectionExtensions (AddMediCore)
│   ├── Exceptions/       HandlerNotFoundException, ValidationException
│   └── MediCore.csproj
├── docs/                 GitHub Pages landing site (see docs/DEPLOY.md)
├── icon/                 NuGet package icon
├── LICENSE.txt           MIT
└── README.md
```

> This repository currently contains only the library itself — there is no sample application or automated test project checked in yet. If you add `samples/` or `tests/` projects, list them here and update the build instructions below.

---

## Design Patterns Used

| Pattern | Where |
|---|---|
| Mediator | Decouples request senders from request handlers |
| Facade | `Mediator` hides dispatching, pipeline construction, and publishing |
| Chain of Responsibility | Interceptors execute as an ordered pipeline |
| Decorator | Each interceptor wraps the next execution stage |
| Strategy | Notification publishing and transaction management are swappable |
| Registry | `ServiceRegistry` stores discovered handlers and interceptors |
| Builder / Options | `MediCoreOptions` provides fluent configuration |
| Adapter | `RequestDispatcher` adapts runtime requests to strongly typed handlers |
| Flyweight | One dispatcher instance is cached and reused per (request, response) type pair |
| Dependency Inversion | Core behavior (transactions, publishing) depends on abstractions |
| CQRS | Commands and queries represent distinct application intentions |

---

## When to Use MediCore

Useful once an application has several distinct operations (`CreateOrder`, `GetOrder`, `CancelOrder`, `SearchOrders`, ...) that each deserve an isolated handler, and/or you need repeatable cross-cutting behavior (validation, logging, transactions) without duplicating it in every service. Works naturally with Clean/Onion/layered architectures, modular monoliths, ASP.NET Core APIs, and background workers.

For a small application with only a couple of operations, calling a service directly may be simpler — MediCore's value grows with the number of operations and cross-cutting concerns.

---

## Building From Source

```bash
git clone https://github.com/VarunSalma/MediCore.git
cd MediCore
dotnet build
```

There is currently no test project in this repository — `dotnet test` will report "no tests to run" unless you've added one.

---

## Important Notes

- **Publish concrete notification types.** Handlers are resolved by the compile-time notification type, so `await publisher.PublishAsync(new OrderPlaced(orderId))`, not a variable typed as `INotification`.
- **Interceptor order matters.** With `Logging → Validation → Transaction`, logging wraps the whole request, validation runs before any transaction begins, and the transaction stays as close to the handler as possible.
- **Replace `TransactionScopeManager`** with your own `ITransactionManager` when you need explicit EF Core / Dapper / ADO.NET transactions instead of an ambient `TransactionScope`.

---

## License

MediCore is licensed under the [MIT License](LICENSE.txt).
