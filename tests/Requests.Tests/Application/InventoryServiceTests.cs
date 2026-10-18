using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Requests.Application.Exceptions;
using Requests.Application.Shelves;
using Requests.Domain.Machines;
using Requests.Infrastructure.Machines;
using Requests.Infrastructure.Persistence;
using Requests.Tests.Infrastructure;

namespace Requests.Tests.Application;

public class InventoryServiceTests : IDisposable
{
    private static readonly DateTime CreatedAt = new(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateTimeOffset Now = new(2026, 10, 18, 9, 30, 0, TimeSpan.Zero);

    private readonly TestDatabase _database = new();

    private readonly FakeTimeProvider _time = new(Now);

    private readonly FakeLogger<InventoryService> _logger = new();

    public InventoryServiceTests()
    {
        AddMachineWithShelf("m-1", "s-1");
        AddMachineWithShelf("m-2", "s-2");
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Load_on_an_empty_shelf_stores_the_quantity_and_updates_shelf_and_machine()
    {
        var response = await NewService().LoadAsync("m-1", Request(quantity: 8), "admin-42", CancellationToken.None);

        Assert.Equal("s-1", response.Id);
        var shelf = await FindShelfAsync("s-1");
        Assert.Equal(8, shelf.Quantity);
        Assert.Equal(Now.UtcDateTime, shelf.UpdatedAt);
        Assert.Equal("admin-42", shelf.UpdatedBy);
        Assert.Equal("admin-7", shelf.CreatedBy);
        using var db = _database.CreateContext();
        var machine = await db.Machines.SingleAsync(m => m.Id == "m-1");
        Assert.Equal(Now.UtcDateTime, machine.UpdatedAt);
        Assert.Equal("admin-42", machine.UpdatedBy);
    }

    [Fact]
    public async Task Load_adds_to_the_current_quantity_and_can_fill_the_shelf_exactly()
    {
        await SetQuantityAsync("s-1", 8);

        await NewService().LoadAsync("m-1", Request(quantity: 2), "admin-42", CancellationToken.None);

        Assert.Equal(10, (await FindShelfAsync("s-1")).Quantity);
    }

    [Fact]
    public async Task Type_mismatch_is_a_validation_error_and_changes_nothing()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => NewService().LoadAsync("m-1", Request(productTypeCode: 2), "admin-42", CancellationToken.None));

        Assert.Equal("productTypeCode", exception.Field);
        Assert.Equal("must be 1, the shelf's product type", exception.Issue);
        Assert.Equal(0, (await FindShelfAsync("s-1")).Quantity);
    }

    [Fact]
    public async Task Load_past_capacity_is_a_conflict_and_changes_nothing()
    {
        await SetQuantityAsync("s-1", 8);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => NewService().LoadAsync("m-1", Request(quantity: 3), "admin-99", CancellationToken.None));

        Assert.Equal("Shelf has room for 2 more items; 3 requested.", exception.Message);
        var shelf = await FindShelfAsync("s-1");
        Assert.Equal(8, shelf.Quantity);
        Assert.Equal("admin-7", shelf.UpdatedBy);
    }

    [Fact]
    public async Task Load_larger_than_an_empty_shelf_is_a_conflict()
    {
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => NewService().LoadAsync("m-1", Request(quantity: 11), "admin-42", CancellationToken.None));

        Assert.Equal("Shelf has room for 10 more items; 11 requested.", exception.Message);
    }

    [Fact]
    public async Task Unknown_machine_is_not_found()
    {
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => NewService().LoadAsync("missing", Request(), "admin-42", CancellationToken.None));

        Assert.Equal("Machine 'missing' was not found.", exception.Message);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("s-2")]
    public async Task Unknown_shelf_or_a_shelf_of_another_machine_is_not_found(string shelfId)
    {
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => NewService().LoadAsync("m-1", Request(shelfId: shelfId), "admin-42", CancellationToken.None));

        Assert.Equal($"Shelf '{shelfId}' was not found on machine 'm-1'.", exception.Message);
        Assert.Equal(0, (await FindShelfAsync("s-2")).Quantity);
    }

    [Fact]
    public async Task Invalid_body_on_an_unknown_machine_is_a_validation_error()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => NewService().LoadAsync("missing", Request(quantity: 0), "admin-42", CancellationToken.None));
    }

    [Fact]
    public async Task Type_mismatch_on_a_full_shelf_is_a_validation_error_not_a_conflict()
    {
        await SetQuantityAsync("s-1", 10);

        await Assert.ThrowsAsync<ValidationException>(
            () => NewService().LoadAsync("m-1", Request(productTypeCode: 2), "admin-42", CancellationToken.None));
    }

    [Fact]
    public async Task Concurrent_loads_never_overfill_the_shelf()
    {
        await SetQuantityAsync("s-1", 8);

        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            using var db = _database.CreateContext();
            try
            {
                await NewService(db).LoadAsync("m-1", Request(quantity: 2), "admin-42", CancellationToken.None);
                return true;
            }
            catch (ConflictException)
            {
                return false;
            }
        })));

        Assert.Equal(1, results.Count(loaded => loaded));
        Assert.Equal(9, results.Count(loaded => !loaded));
        Assert.Equal(10, (await FindShelfAsync("s-1")).Quantity);
    }

    [Fact]
    public async Task Success_is_logged_with_the_load_fields()
    {
        await SetQuantityAsync("s-1", 3);

        await NewService().LoadAsync("m-1", Request(quantity: 4), "admin-42", CancellationToken.None);

        var record = _logger.LatestRecord;
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Equal("m-1", Field(record, "MachineId"));
        Assert.Equal("s-1", Field(record, "ShelfId"));
        Assert.Equal("1", Field(record, "ProductTypeCode"));
        Assert.Equal("4", Field(record, "Quantity"));
        Assert.Equal("7", Field(record, "NewQuantity"));
        Assert.Equal("admin-42", Field(record, "UserId"));
    }

    [Fact]
    public async Task Capacity_exceeded_is_logged_as_a_warning()
    {
        await SetQuantityAsync("s-1", 8);

        await Assert.ThrowsAsync<ConflictException>(
            () => NewService().LoadAsync("m-1", Request(quantity: 3), "admin-42", CancellationToken.None));

        var record = _logger.LatestRecord;
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Equal("8", Field(record, "Quantity"));
        Assert.Equal("10", Field(record, "Capacity"));
        Assert.Equal("3", Field(record, "Requested"));
    }

    [Fact]
    public async Task Type_mismatch_is_logged_as_information()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => NewService().LoadAsync("m-1", Request(productTypeCode: 2), "admin-42", CancellationToken.None));

        var record = _logger.LatestRecord;
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Equal("2", Field(record, "ProductTypeCode"));
        Assert.Equal("s-1", Field(record, "ShelfId"));
    }

    [Fact]
    public async Task Validation_failure_is_logged_with_the_field_name_only()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => NewService().LoadAsync("m-1", Request(quantity: 77), "admin-42", CancellationToken.None));

        var record = _logger.LatestRecord;
        Assert.Equal(LogLevel.Debug, record.Level);
        Assert.Equal("quantity", Field(record, "Field"));
        Assert.DoesNotContain("77", record.Message);
    }

    private InventoryService NewService() => NewService(_database.CreateContext());

    private InventoryService NewService(AppDbContext db) => new(
        new MachineRepository(db),
        new UnitOfWork(db),
        new LoadInventoryRequestValidator(new ShelfSettings()),
        _time,
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

    private static LoadInventoryRequest Request(string shelfId = "s-1", int productTypeCode = 1, int quantity = 5) => new()
    {
        ShelfId = shelfId,
        ProductTypeCode = productTypeCode,
        Quantity = quantity,
    };
}
