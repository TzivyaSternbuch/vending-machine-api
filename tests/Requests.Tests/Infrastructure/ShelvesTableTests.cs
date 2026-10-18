using Microsoft.EntityFrameworkCore;
using Requests.Domain.Machines;

namespace Requests.Tests.Infrastructure;

public class ShelvesTableTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 18, 9, 30, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();

    public ShelvesTableTests()
    {
        using var db = _database.CreateContext();
        db.Machines.Add(NewMachine("m-1"));
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Shelf_is_saved_and_read_back_with_its_machine()
    {
        using (var db = _database.CreateContext())
        {
            db.Add(NewShelf("s-1", "m-1"));
            await db.SaveChangesAsync();
        }

        using var read = _database.CreateContext();
        var machine = await read.Machines.Include(m => m.Shelves).SingleAsync(m => m.Id == "m-1");

        var shelf = Assert.Single(machine.Shelves);
        Assert.Equal("s-1", shelf.Id);
        Assert.Equal(1, shelf.ProductTypeCode);
        Assert.Equal(12, shelf.Capacity);
        Assert.Equal(1.5, shelf.Price);
        Assert.Equal(Now, shelf.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, shelf.CreatedAt.Kind);
        Assert.Equal("admin-42", shelf.CreatedBy);
        Assert.Equal("admin-42", shelf.UpdatedBy);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.5)]
    public async Task Check_constraint_rejects_price_of_0_or_less(double price)
    {
        using var db = _database.CreateContext();
        db.Add(NewShelf("s-1", "m-1", price: price));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Unknown_product_type_is_rejected_by_the_foreign_key()
    {
        using var db = _database.CreateContext();
        db.Add(NewShelf("s-1", "m-1", productTypeCode: 99));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Unknown_machine_is_rejected_by_the_foreign_key()
    {
        using var db = _database.CreateContext();
        db.Add(NewShelf("s-1", "missing"));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Deleting_a_machine_deletes_its_shelves()
    {
        using (var db = _database.CreateContext())
        {
            db.Add(NewShelf("s-1", "m-1"));
            await db.SaveChangesAsync();
        }

        using (var db = _database.CreateContext())
        {
            db.Machines.Remove(await db.Machines.SingleAsync(m => m.Id == "m-1"));
            await db.SaveChangesAsync();
        }

        using var read = _database.CreateContext();
        Assert.Equal(0, await read.Set<Shelf>().CountAsync());
    }

    [Fact]
    public async Task Product_type_used_by_a_shelf_cannot_be_deleted()
    {
        using (var db = _database.CreateContext())
        {
            db.Add(NewShelf("s-1", "m-1"));
            await db.SaveChangesAsync();
        }

        using var delete = _database.CreateContext();
        delete.ProductTypes.Remove(await delete.ProductTypes.SingleAsync(p => p.Code == 1));

        await Assert.ThrowsAsync<DbUpdateException>(() => delete.SaveChangesAsync());
    }

    private static Shelf NewShelf(string id, string machineId, int productTypeCode = 1, int capacity = 12, double price = 1.5) => new()
    {
        Id = id,
        MachineId = machineId,
        ProductTypeCode = productTypeCode,
        Capacity = capacity,
        Price = price,
        CreatedAt = Now,
        CreatedBy = "admin-42",
        UpdatedAt = Now,
        UpdatedBy = "admin-42",
    };

    private static VendingMachine NewMachine(string id) => new()
    {
        Id = id,
        Name = "Riverside Snacks",
        Location = new Location { Region = "Riverside Market", ZipCode = "10001" },
        MaxShelves = 6,
        CreatedAt = Now,
        UpdatedAt = Now,
        CreatedBy = "admin-7",
        UpdatedBy = "admin-7",
    };
}
