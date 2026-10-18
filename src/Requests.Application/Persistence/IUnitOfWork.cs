namespace Requests.Application.Persistence;

public interface IUnitOfWork
{
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
