namespace Requests.Application.Machines;

public class MachineResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required LocationResponse Location { get; init; }

    public int MaxShelves { get; init; }

    public required List<ShelfResponse> Shelves { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public required string CreatedBy { get; init; }

    public required string UpdatedBy { get; init; }
}
