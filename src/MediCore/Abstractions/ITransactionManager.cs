namespace MediCore.Abstractions;

/// <summary>
/// Abstraction over the unit-of-work / transaction technology (System.Transactions, EF Core, Dapper + SqlTransaction, ...).
/// Implement it once and the <c>TransactionInterceptor</c> works with your data access layer.
/// </summary>
public interface ITransactionManager
{
    Task<ITransaction> BeginAsync(CancellationToken cancellationToken);
}

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
