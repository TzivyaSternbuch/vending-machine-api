using Requests.Application.Exceptions;

namespace Requests.Application.Shelves;

// Whether productTypeCode matches the shelf is checked by the service once the shelf is loaded.
public class LoadInventoryRequestValidator(ShelfSettings settings)
{
    public void Validate(LoadInventoryRequest request)
    {
        ValidateShelfId(request.ShelfId);
        ValidateProductTypeCode(request.ProductTypeCode);
        ValidateQuantity(request.Quantity);
    }

    private static void ValidateShelfId(string? shelfId)
    {
        if (string.IsNullOrWhiteSpace(shelfId))
        {
            throw new ValidationException("shelfId", "is required");
        }
    }

    private static void ValidateProductTypeCode(int? productTypeCode)
    {
        if (productTypeCode is null)
        {
            throw new ValidationException("productTypeCode", "is required");
        }
    }

    private void ValidateQuantity(int? quantity)
    {
        if (quantity is null)
        {
            throw new ValidationException("quantity", "is required");
        }

        if (quantity < 1 || quantity > settings.MaxShelfCapacity)
        {
            throw new ValidationException("quantity", $"must be between 1 and {settings.MaxShelfCapacity}");
        }
    }
}
