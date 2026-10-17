namespace Requests.Application.Machines;

public class LocationResponse
{
    public required string Region { get; init; }

    public required string ZipCode { get; init; }

    public string? Description { get; init; }
}
