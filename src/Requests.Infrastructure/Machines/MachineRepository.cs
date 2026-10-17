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
}
