namespace Requests.Application.Products;

public class ProductTypeResponse
{
    public int Code { get; init; }

    public required string Name { get; init; }

    public int CategoryCode { get; init; }
}
