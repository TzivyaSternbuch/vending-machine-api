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

    /// <summary>
    /// A copy that shares no mutable state with this machine. Used only by the in-memory repository
    /// to keep its stored objects isolated; a real database doesn't need it.
    /// </summary>
    public VendingMachine Copy() => new()
    {
        Id = Id,
        Name = Name,
        Location = Location,
        MaxShelves = MaxShelves,
        Shelves = [.. Shelves],
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
        CreatedBy = CreatedBy,
        UpdatedBy = UpdatedBy,
    };
}
