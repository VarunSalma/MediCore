# MediCore

A lightweight, MediatR-style **CQRS mediator for .NET 8** with first-class **interceptors**,
a **service registry**, and strictly segregated interfaces.

```
dotnet build
dotnet test
dotnet run --project samples/MediCore.Sample
```

## Quick start

```csharp
services.AddMediCore(o => o
    .RegisterHandlersFromAssemblyContaining<Program>()
    .AddDefaultInterceptors()                 // Logging -> Validation -> Transaction
    .UseTransactionManager<MyEfTransactionManager>());   // optional

// a command, its handler and its validator
public sealed record CreateOrder(string Customer) : ICommand<Guid>;

public sealed class CreateOrderHandler : ICommandHandler<CreateOrder, Guid>
{
    public Task<Guid> HandleAsync(CreateOrder cmd, CancellationToken ct) => ...;
}

public sealed class CreateOrderValidator : IValidator<CreateOrder> { ... }
services.AddScoped<IValidator<CreateOrder>, CreateOrderValidator>();

// anywhere
var id = await sender.SendAsync(new CreateOrder("Acme"));
```

## Project layout

```
src/MediCore
├── Abstractions/   pure contracts, one responsibility per interface
├── Core/           Mediator (facade), RequestDispatcher (adapter), InterceptorPipeline (chain)
├── Interceptors/   Logging, Validation, Transaction
├── Publishing/     Sequential and Parallel notification strategies
├── Transactions/   TransactionScopeManager (default ITransactionManager)
├── Registry/       IServiceRegistry, scanner, options builder, AddMediCore(...)
└── Exceptions/     HandlerNotFoundException, ValidationException
samples/MediCore.Sample   console demo
tests/MediCore.Tests      xUnit tests
```

## Interface segregation

| Interface | Responsibility | Inject it when you... |
|---|---|---|
| `ISender` | dispatch a request to one handler | only send commands/queries |
| `IPublisher` | publish a notification to many handlers | only raise events |
| `IMediator` | `ISender` + `IPublisher` | need both (rare) |
| `ICommand<T>` / `IQuery<T>` | mark write vs read requests | want CQRS semantics and transaction rules |
| `IRequestHandler` / `ICommandHandler` / `IQueryHandler` | handle exactly one request | implement business logic |
| `IRequestInterceptor` | wrap handler execution | add a cross-cutting concern |
| `IValidator<T>` | validate one request type | validate input |
| `ITransactionManager` / `ITransaction` | begin / commit / roll back | plug in EF Core, Dapper, ADO.NET |
| `INotificationPublisher` | choose HOW handlers run | change the publishing strategy |
| `IServiceRegistry` | read-only catalogue of what was discovered | diagnostics and startup checks |

## Design patterns used

| Pattern | Where |
|---|---|
| Mediator | `Mediator` decouples senders from handlers |
| Facade | `Mediator` hides dispatching, pipeline building and publishing behind two small interfaces |
| Chain of Responsibility / Decorator | `InterceptorPipeline` — each interceptor wraps the next one |
| Strategy | `INotificationPublisher` (sequential / parallel), `ITransactionManager` |
| Registry | `ServiceRegistry` is the single source of truth of handlers and interceptor order |
| Builder / Options | `MediCoreOptions` fluent configuration |
| Adapter / Template Method | `RequestDispatcher<TResponse>` → `RequestDispatcher<TRequest,TResponse>` |
| Flyweight (cache) | dispatchers are created once per request type |
| Dependency Inversion | everything is resolved through abstractions and the container |
| CQRS | `ICommand` / `IQuery` split, transactions only for commands |

## Interceptors 

Execution order = registration order. The first registered is the **outermost**.

```csharp
.AddInterceptor(typeof(LoggingInterceptor<,>))     // 1st  (outermost)
.AddInterceptor(typeof(ValidationInterceptor<,>))  // 2nd
.AddInterceptor(typeof(TransactionInterceptor<,>)) // 3rd  (closest to the handler)
```

Write your own:

```csharp
public sealed class CachingInterceptor<TRequest, TResponse> : IRequestInterceptor<TRequest, TResponse>
    where TRequest : IQuery<TResponse>          // constraint = only queries
{
    public async Task<TResponse> InterceptAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // before ...
        var response = await next();            // skip this line to short-circuit
        // after ...
        return response;
    }
}

o.AddInterceptor(typeof(CachingInterceptor<,>));
```

A generic constraint decides which requests an interceptor applies to. That is how
`TransactionInterceptor` touches commands only.

## Service registry

```csharp
var registry = provider.GetRequiredService<IServiceRegistry>();
registry.RequestHandlers;        // every request -> handler mapping
registry.NotificationHandlers;   // every notification handler
registry.Interceptors;           // interceptors in execution order
```

Startup fails fast if a request has more than one handler.

## Notes

- Publish the concrete notification type (`PublishAsync(new OrderPlaced(...))`); handlers are resolved for the compile-time type.
- Swap `TransactionScopeManager` for your own `ITransactionManager` to use an explicit `DbTransaction` or EF Core transaction.
- Extension ideas: streaming requests, per-request interceptor ordering attributes, notification interceptors, source-generator based registration.
