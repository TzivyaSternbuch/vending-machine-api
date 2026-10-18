using Microsoft.EntityFrameworkCore.Storage;
using Requests.Application.Persistence;

namespace Requests.Infrastructure.Persistence;

public class Transaction(IDbContextTransaction transaction) : ITransaction
{
    public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
