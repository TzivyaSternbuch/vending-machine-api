namespace Requests.Application.Machines;

public class ShelfResponse
{
    public required string Id { get; init; }

    public int ProductTypeCode { get; init; }

    public int Capacity { get; init; }

    public double Price { get; init; }

    public int Quantity { get; init; }
}
