namespace Requests.Application.Purchases;

public class PurchaseResponse
{
    public required string MachineId { get; init; }

    public required string ShelfId { get; init; }

    public int ProductTypeCode { get; init; }

    public double Price { get; init; }

    public int RemainingQuantity { get; init; }
}
