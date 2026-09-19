namespace MediCore.Abstractions;

/// <summary>Handles a single request type.</summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>Handler for a command. Semantic alias of <see cref="IRequestHandler{TRequest,TResponse}"/>.</summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse> { }

/// <summary>Handler for a query. Semantic alias of <see cref="IRequestHandler{TRequest,TResponse}"/>.</summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse> { }
