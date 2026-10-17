using Requests.Domain.Products;

namespace Requests.Application.Products;

public interface IProductTypeRepository
{
    Task<ProductType?> FindByNameAsync(string name, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetAllNamesAsync(CancellationToken cancellationToken);
}
