using System.Collections.Concurrent;
using Requests.Application.Machines;
using Requests.Domain.Machines;

namespace Requests.Infrastructure.Machines;

/// <summary>Keeps machines in memory; everything is lost on restart.</summary>
/// <remarks>
/// Save and find work on copies so a caller can't change a stored machine without calling SaveAsync.
/// This only matters because the store holds live objects; a real database returns fresh objects on
/// every read, so a database repository won't need Copy().
/// </remarks>
public class InMemoryMachineRepository : IMachineRepository
{
    private readonly ConcurrentDictionary<string, VendingMachine> _machines = new();

    public Task SaveAsync(VendingMachine machine, CancellationToken cancellationToken)
    {
        _machines[machine.Id] = machine.Copy();

        return Task.CompletedTask;
    }

    public Task<VendingMachine?> FindByIdAsync(string id, CancellationToken cancellationToken)
    {
        var machine = _machines.TryGetValue(id, out var stored) ? stored.Copy() : null;

        return Task.FromResult(machine);
    }
}
