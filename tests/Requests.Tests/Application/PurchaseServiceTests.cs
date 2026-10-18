using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Requests.Application.Exceptions;
using Requests.Application.Purchases;
using Requests.Domain.Machines;
using Requests.Infrastructure.Machines;
using Requests.Infrastructure.Persistence;
using Requests.Tests.Infrastructure;

namespace Requests.Tests.Application;

public class PurchaseServiceTests : IDisposable
{
    private static readonly DateTime CreatedAt = new(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();

    private readonly FakeLogger<PurchaseService> _logger = new();

    public PurchaseServiceTests()
    {
        AddMachineWithShelf("m-1", "s-1");
        AddMachineWithShelf("m-2", "s-2");
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Purchase_takes_one_item_off_the_shelf()
    {
        await SetQuantityAsync("s-1", 5);

        await NewService().PurchaseAsync("m-1", "s-1", "user-42", CancellationToken.None);

        Assert.Equal(4, (await FindShelfAsync("s-1")).Quantity);
    }

    [Fact]
    public async Task Purchase_returns_the_product_type_price_and_remaining_quantity()
    {
        await SetQuantityAsync("s-1", 5);

        var response = await NewService().PurchaseAsync("m-1", "s-1", "user-42", CancellationToken.None);

        Assert.Equal("m-1", response.MachineId);
        Assert.Equal("s-1", response.ShelfId);
        Assert.Equal(1, response.ProductTypeCode);
        Assert.Equal(4.5, response.Price);
        Assert.Equal(4, response.RemainingQuantity);
    }

    [Fact]
    public async Task Buying_the_last_item_leaves_the_shelf_at_zero()
    {
        await SetQuantityAsync("s-1", 1);

        var response = await NewService().PurchaseAsync("m-1", "s-1", "user-42", CancellationToken.None);

        Assert.Equal(0, response.RemainingQuantity);
        Assert.Equal(0, (await FindShelfAsync("s-1")).Quantity);
    }

    [Fact]
    public async Task Purchase_does_not_change_the_audit_fields()
    {
        await SetQuantityAsync("s-1", 5);

        await NewService().PurchaseAsync("m-1", "s-1", "user-42", CancellationToken.None);

        var shelf = await FindShelfAsync("s-1");
        Assert.Equal(CreatedAt, shelf.UpdatedAt);
        Assert.Equal("admin-7", shelf.UpdatedBy);
        using var db = _database.CreateContext();
        var machine = await db.Machines.SingleAsync(m => m.Id == "m-1");
        Assert.Equal(CreatedAt, machine.UpdatedAt);
        Assert.Equal("admin-7", machine.UpdatedBy);
    }

    [Fact]
    public async Task Shelf_that_was_never_loaded_is_a_conflict()
    {
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => NewService().PurchaseAsync("m-1", "s-1", "user-42", CancellationToken.None));

        Assert.Equal("Shelf s-1 is empty.", exception.Message);
        Assert.Equal(0, (await FindShelfAsync("s-1")).Quantity);
    }

    [Fact]
    public async Task Shelf_that_sold_out_is_a_conflict()
    {
        await SetQuantityAsync("s-1", 1);
        await NewService().PurchaseAsync("m-1", "s-1", "user-42", CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(
            () => NewService().PurchaseAsync("m-1", "s-1", "user-42", CancellationToken.None));

        Assert.Equal(0, (await FindShelfAsync("s-1")).Quantity);
    }

    [Fact]
    public async Task Unknown_machine_is_not_found()
    {
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => NewService().PurchaseAsync("missing", "s-1", "user-42", CancellationToken.None));

        Assert.Equal("Machine 'missing' was not found.", exception.Message);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("s-2")]
    public async Task Unknown_shelf_or_a_shelf_of_another_machine_is_not_found(string shelfId)
    {
        await SetQuantityAsync("s-2", 5);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => NewService().PurchaseAsync("m-1", shelfId, "user-42", CancellationToken.None));

        Assert.Equal($"Shelf '{shelfId}' was not found on machine 'm-1'.", exception.Message);
        Assert.Equal(5, (await FindShelfAsync("s-2")).Quantity);
    }

    [Fact]
    public async Task Concurrent_purchases_never_sell_the_last_item_twice()
    {
        await SetQuantityAsync("s-1", 1);

        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Task.Run(async () =>
        {
            using var db = _database.CreateContext();
            try
            {
                await NewService(db).PurchaseAsync("m-1", "s-1", "user-42", CancellationToken.None);
                return true;
            }
            catch (ConflictException)
            {
                return false;
            }
        })));

        Assert.Equal(1, results.Count(bought => bought));
        Assert.Equal(4, results.Count(bought => !bought));
        Assert.Equal(0, (await FindShelfAsync("s-1")).Quantity);
    }

    [Fact]
    public async Task Success_is_logged_with_the_purchase_fields()
    {
        await SetQuantityAsync("s-1", 5);

        await NewService().PurchaseAsync("m-1", "s-1", "user-42", CancellationToken.None);

        var record = _logger.LatestRecord;
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Equal("m-1", Field(record, "MachineId"));
        Assert.Equal("s-1", Field(record, "ShelfId"));
        Assert.Equal("1", Field(record, "ProductTypeCode"));
        Assert.Equal("4.5", Field(record, "Price"));
        Assert.Equal("4", Field(record, "RemainingQuantity"));
        Assert.Equal("user-42", Field(record, "UserId"));
    }

    [Fact]
    public async Task Empty_shelf_is_logged_as_a_warning()
    {
        await Assert.ThrowsAsync<ConflictException>(
            () => NewService().PurchaseAsync("m-1", "s-1", "user-42", CancellationToken.None));

        var record = _logger.LatestRecord;
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Equal("m-1", Field(record, "MachineId"));
        Assert.Equal("s-1", Field(record, "ShelfId"));
        Assert.Equal("1", Field(record, "ProductTypeCode"));
        Assert.Equal("user-42", Field(record, "UserId"));
    }

    [Fact]
    public async Task Not_found_is_logged_as_information()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => NewService().PurchaseAsync("m-1", "missing", "user-42", CancellationToken.None));

        var record = _logger.LatestRecord;
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Equal("m-1", Field(record, "MachineId"));
        Assert.Equal("missing", Field(record, "ShelfId"));
        Assert.Equal("user-42", Field(record, "UserId"));
    }

    private PurchaseService NewService() => NewService(_database.CreateContext());

    private PurchaseService NewService(AppDbContext db) => new(
        new MachineRepository(db),
        new UnitOfWork(db),
        _logger);

    private void AddMachineWithShelf(string machineId, string shelfId)
    {
        using var db = _database.CreateContext();
        db.Machines.Add(new VendingMachine
        {
            Id = machineId,
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
            Id = shelfId,
            MachineId = machineId,
            ProductTypeCode = 1,
            Capacity = 10,
            Price = 4.5,
            CreatedAt = CreatedAt,
            CreatedBy = "admin-7",
            UpdatedAt = CreatedAt,
            UpdatedBy = "admin-7",
        });
        db.SaveChanges();
    }

    private async Task SetQuantityAsync(string shelfId, int quantity)
    {
        using var db = _database.CreateContext();
        var shelf = await db.Set<Shelf>().SingleAsync(s => s.Id == shelfId);
        shelf.Quantity = quantity;
        await db.SaveChangesAsync();
    }

    private async Task<Shelf> FindShelfAsync(string shelfId)
    {
        using var db = _database.CreateContext();
        return await db.Set<Shelf>().SingleAsync(s => s.Id == shelfId);
    }

    private static string? Field(FakeLogRecord record, string name) =>
        record.StructuredState!.Single(pair => pair.Key == name).Value;
}
