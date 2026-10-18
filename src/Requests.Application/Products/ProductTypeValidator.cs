using Requests.Application.Exceptions;

namespace Requests.Application.Products;


public class ProductTypeValidator(IProductCategoryRepository categories)
{
    public async Task ValidateAsync(CreateProductTypeRequest request, CancellationToken cancellationToken)
    {
        ProductNameRule.Validate(request.Name);
        await ValidateCategoryCodeAsync(request.CategoryCode, cancellationToken);
    }

    private async Task ValidateCategoryCodeAsync(int? categoryCode, CancellationToken cancellationToken)
    {
        if (categoryCode is null)
        {
            throw new ValidationException("categoryCode", "is required");
        }

        if (!await categories.ExistsAsync(categoryCode.Value, cancellationToken))
        {
            var allowed = (await categories.GetAllAsync(cancellationToken)).Select(c => c.Code);
            throw new ValidationException("categoryCode", $"must be one of: {string.Join(", ", allowed)}");
        }
    }
}
