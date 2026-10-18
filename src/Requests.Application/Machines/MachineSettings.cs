namespace Requests.Application.Machines;

public class MachineSettings
{
    public const string MAX_SHELVES_LIMIT_KEY = "MAX_SHELVES_LIMIT";

    public const int DEFAULT_MAX_SHELVES_LIMIT = 20;

    /// <summary>The highest maxShelves a machine may have. Read from config key MAX_SHELVES_LIMIT.</summary>
    public int MaxShelvesLimit { get; init; } = DEFAULT_MAX_SHELVES_LIMIT;
}
