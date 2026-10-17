using Requests.Domain.Machines;

namespace Requests.Application.Machines;

/// <summary>Stores machines.</summary>
public interface IMachineRepository
{
    Task AddAsync(VendingMachine machine, CancellationToken cancellationToken);

    Task<VendingMachine?> FindByIdAsync(string id, CancellationToken cancellationToken);

    Task<VendingMachine?> FindForUpdateAsync(string id, CancellationToken cancellationToken);

    Task<int> CountShelvesAsync(string machineId, CancellationToken cancellationToken);

    void AddShelf(Shelf shelf);
}
