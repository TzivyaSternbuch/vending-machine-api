using Requests.Application.Exceptions;
using Requests.Domain.Products;

namespace Requests.Application.Products;

public class ProductTypeService(IProductTypeRepository repository, ProductTypeValidator validator)
{
    public async Task<ProductTypeResponse> CreateAsync(
        CreateProductTypeRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAsync(request, cancellationToken);

        var name = ProductNameRule.Normalize(request.Name)!;

        if (await repository.ExistsByNameAsync(name, cancellationToken))
        {
            throw new ConflictException($"Product type '{name}' already exists.");
        }

        var productType = new ProductType { Name = name, CategoryCode = request.CategoryCode!.Value };

        await repository.AddAsync(productType, cancellationToken);

        return ToResponse(productType);
    }

    public async Task<List<ProductTypeResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var productTypes = await repository.GetAllAsync(cancellationToken);

        return productTypes.Select(ToResponse).ToList();
    }

    private static ProductTypeResponse ToResponse(ProductType productType) => new()
    {
        Code = productType.Code,
        Name = productType.Name,
        CategoryCode = productType.CategoryCode,
    };
}
