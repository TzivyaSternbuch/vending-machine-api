namespace Requests.Application.Persistence;

// Disposing without committing rolls back.
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
