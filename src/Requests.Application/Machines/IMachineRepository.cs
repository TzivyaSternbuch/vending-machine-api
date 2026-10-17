using Requests.Domain.Machines;

namespace Requests.Application.Machines;

/// <summary>Stores machines. Implementations store and return copies.</summary>
public interface IMachineRepository
{
    Task SaveAsync(VendingMachine machine, CancellationToken cancellationToken);

    Task<VendingMachine?> FindByIdAsync(string id, CancellationToken cancellationToken);
}
