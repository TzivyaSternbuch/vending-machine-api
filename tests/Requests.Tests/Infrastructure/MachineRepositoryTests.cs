using Requests.Domain.Machines;
using Requests.Infrastructure.Machines;

namespace Requests.Tests.Infrastructure;

public class MachineRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public async Task FindById_returns_saved_machine()
    {
        await NewRepository().AddAsync(NewMachine("m-1"), CancellationToken.None);

        var found = await NewRepository().FindByIdAsync("m-1", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("m-1", found.Id);
        Assert.Equal("Riverside Snacks", found.Name);
        Assert.Equal("Riverside Market", found.Location.Region);
        Assert.Equal("10001", found.Location.ZipCode);
        Assert.Equal("by the well", found.Location.Description);
        Assert.Equal(6, found.MaxShelves);
        Assert.Empty(found.Shelves);
        Assert.Equal(new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc), found.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, found.CreatedAt.Kind);
        Assert.Equal("admin-42", found.CreatedBy);
    }

    [Fact]
    public async Task FindById_returns_null_for_unknown_id()
    {
        var found = await NewRepository().FindByIdAsync("missing", CancellationToken.None);

        Assert.Null(found);
    }

    public void Dispose() => _database.Dispose();

    private MachineRepository NewRepository() => new(_database.CreateContext());

    private static VendingMachine NewMachine(string id) => new()
    {
        Id = id,
        Name = "Riverside Snacks",
        Location = new Location { Region = "Riverside Market", ZipCode = "10001", Description = "by the well" },
        MaxShelves = 6,
        CreatedAt = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc),
        CreatedBy = "admin-42",
        UpdatedBy = "admin-42",
    };
}
