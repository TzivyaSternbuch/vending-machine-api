using Requests.Domain.Machines;
using Requests.Infrastructure.Machines;

namespace Requests.Tests.Infrastructure;

public class InMemoryMachineRepositoryTests
{
    private readonly InMemoryMachineRepository _repository = new();

    [Fact]
    public async Task FindById_returns_saved_machine()
    {
        var machine = NewMachine("m-1");

        await _repository.SaveAsync(machine, CancellationToken.None);
        var found = await _repository.FindByIdAsync("m-1", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("m-1", found.Id);
        Assert.Equal("Riverside Snacks", found.Name);
        Assert.Equal("10001", found.Location.ZipCode);
        Assert.Equal(6, found.MaxShelves);
        Assert.Empty(found.Shelves);
        Assert.Equal("admin-42", found.CreatedBy);
    }

    [Fact]
    public async Task FindById_returns_null_for_unknown_id()
    {
        var found = await _repository.FindByIdAsync("missing", CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task Changing_saved_object_does_not_change_store()
    {
        var machine = NewMachine("m-1");
        await _repository.SaveAsync(machine, CancellationToken.None);

        machine.Name = "Changed";
        machine.Shelves.Add(new Shelf());

        var found = await _repository.FindByIdAsync("m-1", CancellationToken.None);
        Assert.Equal("Riverside Snacks", found!.Name);
        Assert.Empty(found.Shelves);
    }

    [Fact]
    public async Task Changing_returned_object_does_not_change_store()
    {
        await _repository.SaveAsync(NewMachine("m-1"), CancellationToken.None);

        var first = await _repository.FindByIdAsync("m-1", CancellationToken.None);
        first!.Name = "Changed";
        first.Shelves.Add(new Shelf());

        var second = await _repository.FindByIdAsync("m-1", CancellationToken.None);
        Assert.Equal("Riverside Snacks", second!.Name);
        Assert.Empty(second.Shelves);
    }

    [Fact]
    public async Task Parallel_saves_are_all_stored()
    {
        var ids = Enumerable.Range(0, 1000).Select(i => $"m-{i}").ToList();

        await Task.WhenAll(ids.Select(id => Task.Run(() => _repository.SaveAsync(NewMachine(id), CancellationToken.None))));

        foreach (var id in ids)
        {
            Assert.NotNull(await _repository.FindByIdAsync(id, CancellationToken.None));
        }
    }

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
