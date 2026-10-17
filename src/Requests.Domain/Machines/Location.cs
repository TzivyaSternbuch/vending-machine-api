namespace Requests.Domain.Machines;

/// <summary>Where a vending machine stands.</summary>
public class Location
{
    public required string Region { get; init; }

    /// <summary>Digits only; a string so leading zeros stay.</summary>
    public required string ZipCode { get; init; }

    public string? Description { get; init; }
}
