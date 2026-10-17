using Microsoft.EntityFrameworkCore;
using Requests.Application.Products;
using Requests.Domain.Products;
using Requests.Infrastructure.Persistence;

namespace Requests.Infrastructure.Products;

public class ProductTypeRepository(AppDbContext db) : IProductTypeRepository
{
    public async Task AddAsync(ProductType productType, CancellationToken cancellationToken)
    {
        db.ProductTypes.Add(productType);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken) =>
        db.ProductTypes.AnyAsync(p => p.Name == name, cancellationToken);

    public async Task<IReadOnlyList<ProductType>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.ProductTypes.AsNoTracking().OrderBy(p => p.Code).ToListAsync(cancellationToken);
}
