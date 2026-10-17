namespace Requests.Domain.Products;

/// <summary>A group of product types, e.g. "drink". Reference data.</summary>
public class ProductCategory
{
    public required int Code { get; init; }

    public required string Name { get; init; }
}
