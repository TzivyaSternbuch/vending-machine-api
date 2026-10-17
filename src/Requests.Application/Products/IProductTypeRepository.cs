using Requests.Domain.Products;

namespace Requests.Application.Products;

public interface IProductTypeRepository
{
    Task AddAsync(ProductType productType, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int code, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductType>> GetAllAsync(CancellationToken cancellationToken);
}
