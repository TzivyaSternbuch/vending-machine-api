using Microsoft.EntityFrameworkCore;
using Requests.Application.Machines;
using Requests.Domain.Machines;
using Requests.Infrastructure.Persistence;

namespace Requests.Infrastructure.Machines;

public class MachineRepository(AppDbContext db) : IMachineRepository
{
    public async Task AddAsync(VendingMachine machine, CancellationToken cancellationToken)
    {
        db.Machines.Add(machine);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<VendingMachine?> FindByIdAsync(string id, CancellationToken cancellationToken) =>
        db.Machines.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<VendingMachine?> FindForUpdateAsync(string id, CancellationToken cancellationToken) =>
        db.Machines.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<int> CountShelvesAsync(string machineId, CancellationToken cancellationToken) =>
        db.Set<Shelf>().CountAsync(s => s.MachineId == machineId, cancellationToken);

    public void AddShelf(Shelf shelf) => db.Set<Shelf>().Add(shelf);
}
