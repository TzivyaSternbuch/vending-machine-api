namespace Requests.Application.Shelves;

public class ShelfSettings
{
    public const string MAX_SHELF_CAPACITY_KEY = "MAX_SHELF_CAPACITY";

    public const int DEFAULT_MAX_SHELF_CAPACITY = 50;

    public int MaxShelfCapacity { get; init; } = DEFAULT_MAX_SHELF_CAPACITY;
}