using MediCore.Abstractions;

namespace MediCore.Interceptors;

/// <summary>
/// Wraps every <see cref="ICommand{TResponse}"/> in a transaction: commit on success, rollback on exception.
/// The generic constraint means queries and plain requests are never touched.
/// </summary>
public sealed class TransactionInterceptor<TRequest, TResponse> : IRequestInterceptor<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly ITransactionManager _transactions;

    public TransactionInterceptor(ITransactionManager transactions) => _transactions = transactions;

    public async Task<TResponse> InterceptAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _transactions.BeginAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var response = await next().ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
