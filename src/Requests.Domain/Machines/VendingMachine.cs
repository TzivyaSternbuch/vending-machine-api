namespace Requests.Domain.Machines;

public class VendingMachine
{
    public const int DEFAULT_MAX_SHELVES = 20;

    public required string Id { get; init; }

    public required string Name { get; set; }

    public required Location Location { get; set; }

    public int MaxShelves { get; set; } = DEFAULT_MAX_SHELVES;

    public List<Shelf> Shelves { get; set; } = [];

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; set; }

    public required string CreatedBy { get; init; }

    public required string UpdatedBy { get; set; }
}
