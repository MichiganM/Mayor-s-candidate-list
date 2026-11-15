namespace MayorElection.Domain.Repositories;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    // Several saves (e.g. list reordering) can share one atomic transaction.
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}
