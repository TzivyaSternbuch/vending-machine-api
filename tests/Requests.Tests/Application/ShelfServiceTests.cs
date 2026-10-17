using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Requests.Application.Exceptions;
using Requests.Application.Shelves;
using Requests.Domain.Machines;
using Requests.Infrastructure.Machines;
using Requests.Infrastructure.Persistence;
using Requests.Infrastructure.Products;
using Requests.Tests.Infrastructure;

namespace Requests.Tests.Application;

public class ShelfServiceTests : IDisposable
{
    private static readonly DateTimeOffset MachineCreatedAt = new(2026, 10, 15, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Now = new(2026, 10, 17, 9, 30, 0, TimeSpan.Zero);

    private readonly TestDatabase _database = new();

    private readonly FakeTimeProvider _time = new(Now);

    private readonly FakeLogger<ShelfService> _logger = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Add_stores_and_returns_the_shelf_and_updates_the_machine()
    {
        await CreateMachineAsync("m-1", maxShelves: 6);

        var response = await NewService().AddAsync("m-1", Request(), "admin-42", CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(response.Id));
        Assert.Equal(1, response.ProductTypeCode);
        Assert.Equal(12, response.Capacity);
        Assert.Equal(1.5, response.Price);

        using var db = _database.CreateContext();
        var machine = await db.Machines.Include(m => m.Shelves).SingleAsync(m => m.Id == "m-1");
        var shelf = Assert.Single(machine.Shelves);
        Assert.Equal(response.Id, shelf.Id);
        Assert.Equal(Now.UtcDateTime, shelf.CreatedAt);
        Assert.Equal(Now.UtcDateTime, shelf.UpdatedAt);
        Assert.Equal("admin-42", shelf.CreatedBy);
        Assert.Equal("admin-42", shelf.UpdatedBy);
        Assert.Equal(Now.UtcDateTime, machine.UpdatedAt);
        Assert.Equal("admin-42", machine.UpdatedBy);
        Assert.Equal(MachineCreatedAt.UtcDateTime, machine.CreatedAt);
        Assert.Equal("admin-7", machine.CreatedBy);
    }

    [Fact]
    public async Task Each_shelf_gets_its_own_id()
    {
        await CreateMachineAsync("m-1", maxShelves: 6);

        var first = await NewService().AddAsync("m-1", Request(), "admin-42", CancellationToken.None);
        var second = await NewService().AddAsync("m-1", Request(), "admin-42", CancellationToken.None);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task Adding_up_to_maxShelves_succeeds()
    {
        await CreateMachineAsync("m-1", maxShelves: 3);

        await AddShelvesAsync("m-1", 3);

        Assert.Equal(3, await CountShelvesAsync("m-1"));
    }

    [Fact]
    public async Task Adding_past_maxShelves_is_a_conflict_and_stores_nothing()
    {
        await CreateMachineAsync("m-1", maxShelves: 3);
        await AddShelvesAsync("m-1", 3);
        _time.Advance(TimeSpan.FromHours(1));

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => NewService().AddAsync("m-1", Request(), "admin-99", CancellationToken.None));

        Assert.Equal("Machine already has its maximum of 3 shelves.", exception.Message);
        Assert.Equal(3, await CountShelvesAsync("m-1"));
        using var db = _database.CreateContext();
        var machine = await db.Machines.SingleAsync(m => m.Id == "m-1");
        Assert.Equal(Now.UtcDateTime, machine.UpdatedAt);
        Assert.Equal("admin-42", machine.UpdatedBy);
    }

    [Fact]
    public async Task Default_limit_allows_20_shelves()
    {
        await CreateMachineAsync("m-1", maxShelves: VendingMachine.DEFAULT_MAX_SHELVES);

        await AddShelvesAsync("m-1", 20);

        await Assert.ThrowsAsync<ConflictException>(
            () => NewService().AddAsync("m-1", Request(), "admin-42", CancellationToken.None));
        Assert.Equal(20, await CountShelvesAsync("m-1"));
    }

    [Fact]
    public async Task Limit_of_1_allows_one_shelf()
    {
        await CreateMachineAsync("m-1", maxShelves: 1);

        await NewService().AddAsync("m-1", Request(), "admin-42", CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(
            () => NewService().AddAsync("m-1", Request(), "admin-42", CancellationToken.None));
    }

    [Fact]
    public async Task Limit_counts_only_this_machines_shelves()
    {
        await CreateMachineAsync("m-1", maxShelves: 1);
        await CreateMachineAsync("m-2", maxShelves: 1);
        await NewService().AddAsync("m-1", Request(), "admin-42", CancellationToken.None);

        await NewService().AddAsync("m-2", Request(), "admin-42", CancellationToken.None);

        Assert.Equal(1, await CountShelvesAsync("m-2"));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("not a valid id !")]
    public async Task Unknown_machine_is_not_found(string machineId)
    {
        await CreateMachineAsync("m-1", maxShelves: 6);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => NewService().AddAsync(machineId, Request(), "admin-42", CancellationToken.None));

        Assert.Equal($"Machine '{machineId}' was not found.", exception.Message);
    }

    [Fact]
    public async Task Invalid_body_on_a_full_machine_is_a_validation_error()
    {
        await CreateMachineAsync("m-1", maxShelves: 1);
        await AddShelvesAsync("m-1", 1);

        await Assert.ThrowsAsync<ValidationException>(
            () => NewService().AddAsync("m-1", Request(price: 0), "admin-42", CancellationToken.None));
    }

    [Fact]
    public async Task Invalid_body_on_an_unknown_machine_is_a_validation_error()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => NewService().AddAsync("missing", Request(productTypeCode: 99), "admin-42", CancellationToken.None));
    }

    [Fact]
    public async Task Concurrent_adds_never_pass_the_limit()
    {
        await CreateMachineAsync("m-1", maxShelves: 6);
        await AddShelvesAsync("m-1", 5);

        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            using var db = _database.CreateContext();
            try
            {
                await NewService(db).AddAsync("m-1", Request(), "admin-42", CancellationToken.None);
                return true;
            }
            catch (ConflictException)
            {
                return false;
            }
        })));

        Assert.Equal(1, results.Count(added => added));
        Assert.Equal(9, results.Count(added => !added));
        Assert.Equal(6, await CountShelvesAsync("m-1"));
    }

    [Fact]
    public async Task Success_is_logged_with_the_shelf_fields()
    {
        await CreateMachineAsync("m-1", maxShelves: 6);

        var response = await NewService().AddAsync("m-1", Request(), "admin-42", CancellationToken.None);

        var record = _logger.LatestRecord;
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Equal(response.Id, Field(record, "ShelfId"));
        Assert.Equal("m-1", Field(record, "MachineId"));
        Assert.Equal("admin-42", Field(record, "UserId"));
        Assert.Equal("1", Field(record, "ProductTypeCode"));
        Assert.Equal("12", Field(record, "Capacity"));
    }

    [Fact]
    public async Task Limit_reached_is_logged_as_a_warning()
    {
        await CreateMachineAsync("m-1", maxShelves: 1);
        await AddShelvesAsync("m-1", 1);

        await Assert.ThrowsAsync<ConflictException>(
            () => NewService().AddAsync("m-1", Request(), "admin-42", CancellationToken.None));

        var record = _logger.LatestRecord;
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Equal("1", Field(record, "ShelfCount"));
        Assert.Equal("1", Field(record, "MaxShelves"));
    }

    [Fact]
    public async Task Validation_failure_is_logged_with_the_field_name_only()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => NewService().AddAsync("m-1", Request(price: -7.25), "admin-42", CancellationToken.None));

        var record = _logger.LatestRecord;
        Assert.Equal(LogLevel.Debug, record.Level);
        Assert.Equal("price", Field(record, "Field"));
        Assert.DoesNotContain("7.25", record.Message);
    }

    private ShelfService NewService() => NewService(_database.CreateContext());

    private ShelfService NewService(AppDbContext db) => new(
        new MachineRepository(db),
        new UnitOfWork(db),
        new AddShelfRequestValidator(new ProductTypeRepository(db), new ShelfSettings()),
        _time,
        _logger);

    private async Task CreateMachineAsync(string id, int maxShelves)
    {
        await new MachineRepository(_database.CreateContext()).AddAsync(
            new VendingMachine
            {
                Id = id,
                Name = "Riverside Snacks",
                Location = new Location { Region = "Riverside Market", ZipCode = "10001" },
                MaxShelves = maxShelves,
                CreatedAt = MachineCreatedAt.UtcDateTime,
                UpdatedAt = MachineCreatedAt.UtcDateTime,
                CreatedBy = "admin-7",
                UpdatedBy = "admin-7",
            },
            CancellationToken.None);
    }

    private async Task AddShelvesAsync(string machineId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await NewService().AddAsync(machineId, Request(), "admin-42", CancellationToken.None);
        }
    }

    private async Task<int> CountShelvesAsync(string machineId)
    {
        using var db = _database.CreateContext();
        return await db.Set<Shelf>().CountAsync(s => s.MachineId == machineId);
    }

    private static string? Field(FakeLogRecord record, string name) =>
        record.StructuredState!.Single(pair => pair.Key == name).Value;

    private static AddShelfRequest Request(int productTypeCode = 1, int capacity = 12, double price = 1.5) => new()
    {
        ProductTypeCode = productTypeCode,
        Capacity = capacity,
        Price = price,
    };
}
