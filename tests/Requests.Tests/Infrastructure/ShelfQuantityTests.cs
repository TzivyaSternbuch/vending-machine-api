using Microsoft.EntityFrameworkCore;
using Requests.Domain.Machines;

namespace Requests.Tests.Infrastructure;

// The CK_Shelves_Quantity check constraint, which backs up the service's capacity rule.
public class ShelfQuantityTests : IDisposable
{
    private static readonly DateTime CreatedAt = new(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task New_shelf_starts_empty()
    {
        await AddShelfAsync(capacity: 10);

        using var db = _database.CreateContext();
        Assert.Equal(0, (await db.Set<Shelf>().SingleAsync()).Quantity);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public async Task Quantity_up_to_capacity_is_saved(int quantity)
    {
        await AddShelfAsync(capacity: 10);

        await SetQuantityAsync(quantity);

        using var db = _database.CreateContext();
        Assert.Equal(quantity, (await db.Set<Shelf>().SingleAsync()).Quantity);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(-1)]
    public async Task Quantity_above_capacity_or_below_0_is_rejected(int quantity)
    {
        await AddShelfAsync(capacity: 10);

        await Assert.ThrowsAsync<DbUpdateException>(() => SetQuantityAsync(quantity));
    }

    [Fact]
    public async Task Empty_shelf_with_capacity_0_is_saved()
    {
        await AddShelfAsync(capacity: 0);

        using var db = _database.CreateContext();
        Assert.Equal(1, await db.Set<Shelf>().CountAsync());
    }

    [Fact]
    public async Task Empty_shelf_with_negative_capacity_is_rejected()
    {
        await Assert.ThrowsAsync<DbUpdateException>(() => AddShelfAsync(capacity: -1));
    }

    private async Task AddShelfAsync(int capacity)
    {
        using var db = _database.CreateContext();
        db.Machines.Add(new VendingMachine
        {
            Id = "m-1",
            Name = "Riverside Snacks",
            Location = new Location { Region = "Riverside Market", ZipCode = "10001" },
            MaxShelves = 6,
            CreatedAt = CreatedAt,
            UpdatedAt = CreatedAt,
            CreatedBy = "admin-7",
            UpdatedBy = "admin-7",
        });
        db.Set<Shelf>().Add(new Shelf
        {
            Id = "s-1",
            MachineId = "m-1",
            ProductTypeCode = 1,
            Capacity = capacity,
            Price = 4.5,
            CreatedAt = CreatedAt,
            CreatedBy = "admin-7",
            UpdatedAt = CreatedAt,
            UpdatedBy = "admin-7",
        });
        await db.SaveChangesAsync();
    }

    private async Task SetQuantityAsync(int quantity)
    {
        using var db = _database.CreateContext();
        var shelf = await db.Set<Shelf>().SingleAsync();
        shelf.Quantity = quantity;
        await db.SaveChangesAsync();
    }
}
