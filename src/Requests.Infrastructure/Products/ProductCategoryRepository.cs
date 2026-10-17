using Microsoft.EntityFrameworkCore;
using Requests.Application.Products;
using Requests.Domain.Products;
using Requests.Infrastructure.Persistence;

namespace Requests.Infrastructure.Products;

public class ProductCategoryRepository(AppDbContext db) : IProductCategoryRepository
{
    public async Task AddAsync(ProductCategory category, CancellationToken cancellationToken)
    {
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(int code, CancellationToken cancellationToken) =>
        db.ProductCategories.AnyAsync(c => c.Code == code, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken) =>
        db.ProductCategories.AnyAsync(c => c.Name == name, cancellationToken);

    public async Task<IReadOnlyList<ProductCategory>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.ProductCategories.AsNoTracking().OrderBy(c => c.Code).ToListAsync(cancellationToken);
}
