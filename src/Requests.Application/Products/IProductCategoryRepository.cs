using Requests.Domain.Products;

namespace Requests.Application.Products;

public interface IProductCategoryRepository
{
    Task AddAsync(ProductCategory category, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int code, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductCategory>> GetAllAsync(CancellationToken cancellationToken);
}
