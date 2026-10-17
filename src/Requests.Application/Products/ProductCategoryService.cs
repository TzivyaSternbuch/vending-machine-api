using Requests.Application.Exceptions;
using Requests.Domain.Products;

namespace Requests.Application.Products;

public class ProductCategoryService(IProductCategoryRepository repository, ProductCategoryValidator validator)
{
    public async Task<ProductCategoryResponse> CreateAsync(
        CreateProductCategoryRequest request,
        CancellationToken cancellationToken)
    {
        validator.Validate(request);

        var name = ProductNameRule.Normalize(request.Name)!;

        if (await repository.ExistsByNameAsync(name, cancellationToken))
        {
            throw new ConflictException($"Product category '{name}' already exists.");
        }

        var category = new ProductCategory { Name = name };

        await repository.AddAsync(category, cancellationToken);

        return ToResponse(category);
    }

    public async Task<List<ProductCategoryResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var categories = await repository.GetAllAsync(cancellationToken);

        return categories.Select(ToResponse).ToList();
    }

    private static ProductCategoryResponse ToResponse(ProductCategory category) => new()
    {
        Code = category.Code,
        Name = category.Name,
    };
}
