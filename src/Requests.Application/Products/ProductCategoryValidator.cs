namespace Requests.Application.Products;

public class ProductCategoryValidator
{
    public void Validate(CreateProductCategoryRequest request)
    {
        ProductNameRule.Validate(request.Name);
    }
}
