namespace Requests.Domain.Machines;

public class Shelf
{
    public required string Id { get; init; }

    public required string MachineId { get; init; }

    public required int ProductTypeCode { get; init; }

    public int Capacity { get; init; }

    public double Price { get; init; }

    public int Quantity { get; set; }

    public DateTime CreatedAt { get; init; }

    public required string CreatedBy { get; init; }

    public DateTime UpdatedAt { get; set; }

    public required string UpdatedBy { get; set; }
}
