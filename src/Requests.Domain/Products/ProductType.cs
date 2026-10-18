namespace Requests.Domain.Products;

/// <summary>What a shelf holds, e.g. "water". Reference data.</summary>
public class ProductType
{
    /// <summary>Assigned by the database.</summary>
    public int Code { get; init; }

    public required string Name { get; init; }

    public required int CategoryCode { get; init; }
}
