using Microsoft.EntityFrameworkCore;
using Requests.Application.Products;
using Requests.Domain.Products;
using Requests.Infrastructure.Persistence;

namespace Requests.Infrastructure.Products;

public class ProductTypeRepository(AppDbContext db) : IProductTypeRepository
{
    public Task<ProductType?> FindByNameAsync(string name, CancellationToken cancellationToken) =>
        db.ProductTypes.AsNoTracking().FirstOrDefaultAsync(p => p.Name == name, cancellationToken);

    public async Task<IReadOnlyList<string>> GetAllNamesAsync(CancellationToken cancellationToken) =>
        await db.ProductTypes.OrderBy(p => p.Name).Select(p => p.Name).ToListAsync(cancellationToken);
}
