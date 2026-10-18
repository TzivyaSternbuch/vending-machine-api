using Requests.Application.Persistence;

namespace Requests.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    // Microsoft.Data.Sqlite starts it as BEGIN IMMEDIATE: one writer at a time, so a read inside it can't go stale.
    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        new Transaction(await db.Database.BeginTransactionAsync(cancellationToken));

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
