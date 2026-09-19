using System.Transactions;
using MediCore.Abstractions;

namespace MediCore.Transactions;

/// <summary>
/// Default <see cref="ITransactionManager"/> based on <see cref="TransactionScope"/> (ambient transaction).
/// Works with ADO.NET / Dapper / Entity Framework connections opened inside the scope.
/// Replace it (UseTransactionManager&lt;T&gt;) for an explicit DbTransaction or EF Core transaction.
/// </summary>
public sealed class TransactionScopeManager : ITransactionManager
{
    public Task<ITransaction> BeginAsync(CancellationToken cancellationToken)
        => Task.FromResult<ITransaction>(new AmbientTransaction());

    private sealed class AmbientTransaction : ITransaction
    {
        private TransactionScope? _scope = new(
            TransactionScopeOption.Required,
            new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.FromSeconds(30)
            },
            TransactionScopeAsyncFlowOption.Enabled);

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            _scope?.Complete();
            return Task.CompletedTask;
        }

        // Not calling Complete() makes the scope roll back when it is disposed.
        public Task RollbackAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public ValueTask DisposeAsync()
        {
            _scope?.Dispose();
            _scope = null;
            return ValueTask.CompletedTask;
        }
    }
}
