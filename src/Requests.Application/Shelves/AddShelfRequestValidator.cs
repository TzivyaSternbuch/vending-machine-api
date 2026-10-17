using Requests.Application.Exceptions;
using Requests.Application.Products;

namespace Requests.Application.Shelves;

public class AddShelfRequestValidator(IProductTypeRepository productTypes, ShelfSettings settings)
{
    public async Task ValidateAsync(AddShelfRequest request, CancellationToken cancellationToken)
    {
        await ValidateProductTypeCodeAsync(request.ProductTypeCode, cancellationToken);
        ValidateCapacity(request.Capacity);
        ValidatePrice(request.Price);
    }

    private async Task ValidateProductTypeCodeAsync(int? productTypeCode, CancellationToken cancellationToken)
    {
        if (productTypeCode is null)
        {
            throw new ValidationException("productTypeCode", "is required");
        }

        if (!await productTypes.ExistsAsync(productTypeCode.Value, cancellationToken))
        {
            var allowed = (await productTypes.GetAllAsync(cancellationToken)).Select(p => p.Code);
            throw new ValidationException("productTypeCode", $"must be one of: {string.Join(", ", allowed)}");
        }
    }

    private void ValidateCapacity(int? capacity)
    {
        if (capacity is null)
        {
            throw new ValidationException("capacity", "is required");
        }

        if (capacity > settings.MaxShelfCapacity)
        {
            throw new ValidationException("capacity", $"must be at most {settings.MaxShelfCapacity}");
        }
    }

    private static void ValidatePrice(double? price)
    {
        if (price is null)
        {
            throw new ValidationException("price", "is required");
        }

        if (price <= 0)
        {
            throw new ValidationException("price", "must be greater than 0");
        }
    }
}
