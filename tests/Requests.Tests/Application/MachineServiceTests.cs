using Microsoft.Extensions.Time.Testing;
using Requests.Application.Exceptions;
using Requests.Application.Machines;
using Requests.Infrastructure.Machines;

namespace Requests.Tests.Application;

public class MachineServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly InMemoryMachineRepository _repository = new();

    private readonly MachineService _service;

    public MachineServiceTests()
    {
        var validator = new MachineValidator(new MachineSettings());
        _service = new MachineService(_repository, validator, new FakeTimeProvider(Now));
    }

    [Fact]
    public async Task Create_with_every_field_stores_and_returns_machine()
    {
        var response = await _service.CreateAsync(ValidRequest(), "admin-42", CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(response.Id));
        Assert.Equal("Riverside Snacks", response.Name);
        Assert.Equal("Riverside Market", response.Location.Region);
        Assert.Equal("10001", response.Location.ZipCode);
        Assert.Equal("by the well", response.Location.Description);
        Assert.Equal(6, response.MaxShelves);
        Assert.Empty(response.Shelves);
        Assert.Equal(Now.UtcDateTime, response.CreatedAt);
        Assert.Equal(Now.UtcDateTime, response.UpdatedAt);
        Assert.Equal("admin-42", response.CreatedBy);
        Assert.Equal("admin-42", response.UpdatedBy);

        var stored = await _repository.FindByIdAsync(response.Id, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("Riverside Snacks", stored.Name);
    }

    [Fact]
    public async Task Create_without_description_stores_null()
    {
        var request = ValidRequest();
        request.Location!.Description = null;

        var response = await _service.CreateAsync(request, "admin-42", CancellationToken.None);

        Assert.Null(response.Location.Description);
    }

    [Fact]
    public async Task Missing_maxShelves_defaults_to_20()
    {
        var request = new CreateMachineRequest
        {
            Name = "Riverside Snacks",
            Location = new LocationRequest { Region = "Riverside Market", ZipCode = "10001" },
        };

        var response = await _service.CreateAsync(request, "admin-42", CancellationToken.None);

        Assert.Equal(20, response.MaxShelves);
    }

    [Fact]
    public async Task Duplicate_names_get_different_ids()
    {
        var first = await _service.CreateAsync(ValidRequest(), "admin-42", CancellationToken.None);
        var second = await _service.CreateAsync(ValidRequest(), "admin-42", CancellationToken.None);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task Values_are_stored_trimmed()
    {
        var request = ValidRequest();
        request.Name = "  Riverside Snacks  ";
        request.Location!.Region = "  Riverside Market ";
        request.Location.Description = " by the well ";

        var response = await _service.CreateAsync(request, "admin-42", CancellationToken.None);

        Assert.Equal("Riverside Snacks", response.Name);
        Assert.Equal("Riverside Market", response.Location.Region);
        Assert.Equal("by the well", response.Location.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Riverside\u0007Snacks")]
    public async Task Invalid_name_is_rejected(string? name)
    {
        var request = ValidRequest();
        request.Name = name;

        await AssertRejectedAsync(request, "name");
    }

    [Fact]
    public async Task Name_of_65_characters_is_rejected()
    {
        var request = ValidRequest();
        request.Name = new string('a', 65);

        await AssertRejectedAsync(request, "name");
    }

    [Fact]
    public async Task Name_of_64_characters_is_accepted()
    {
        var request = ValidRequest();
        request.Name = new string('a', 64);

        var response = await _service.CreateAsync(request, "admin-42", CancellationToken.None);

        Assert.Equal(64, response.Name.Length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public async Task MaxShelves_at_limits_is_accepted(int maxShelves)
    {
        var request = ValidRequest();
        request.MaxShelves = maxShelves;

        var response = await _service.CreateAsync(request, "admin-42", CancellationToken.None);

        Assert.Equal(maxShelves, response.MaxShelves);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(-1)]
    public async Task MaxShelves_out_of_range_is_rejected(int maxShelves)
    {
        var request = ValidRequest();
        request.MaxShelves = maxShelves;

        await AssertRejectedAsync(request, "maxShelves");
    }

    [Fact]
    public async Task MaxShelves_limit_comes_from_settings()
    {
        var service = new MachineService(
            _repository,
            new MachineValidator(new MachineSettings { MaxShelvesLimit = 5 }),
            new FakeTimeProvider(Now));
        var request = ValidRequest();
        request.MaxShelves = 6;

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(request, "admin-42", CancellationToken.None));

        Assert.Equal("maxShelves", exception.Field);
    }

    [Fact]
    public async Task Missing_location_is_rejected()
    {
        var request = ValidRequest();
        request.Location = null;

        await AssertRejectedAsync(request, "location");
    }

    [Fact]
    public async Task Empty_location_is_rejected()
    {
        var request = ValidRequest();
        request.Location = new LocationRequest();

        await AssertRejectedAsync(request, "location.region");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Invalid_region_is_rejected(string? region)
    {
        var request = ValidRequest();
        request.Location!.Region = region;

        await AssertRejectedAsync(request, "location.region");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("1000A")]
    [InlineData("1234")]
    [InlineData("12345678901")]
    [InlineData(" 10001")]
    public async Task Invalid_zipCode_is_rejected(string? zipCode)
    {
        var request = ValidRequest();
        request.Location!.ZipCode = zipCode;

        await AssertRejectedAsync(request, "location.zipCode");
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567890")]
    [InlineData("01234")]
    public async Task Valid_zipCode_is_stored_unchanged(string zipCode)
    {
        var request = ValidRequest();
        request.Location!.ZipCode = zipCode;

        var response = await _service.CreateAsync(request, "admin-42", CancellationToken.None);

        Assert.Equal(zipCode, response.Location.ZipCode);
    }

    [Fact]
    public async Task Description_over_256_characters_is_rejected()
    {
        var request = ValidRequest();
        request.Location!.Description = new string('a', 257);

        await AssertRejectedAsync(request, "location.description");
    }

    private async Task AssertRejectedAsync(CreateMachineRequest request, string expectedField)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(request, "admin-42", CancellationToken.None));

        Assert.Equal(expectedField, exception.Field);
    }

    private static CreateMachineRequest ValidRequest() => new()
    {
        Name = "Riverside Snacks",
        Location = new LocationRequest { Region = "Riverside Market", ZipCode = "10001", Description = "by the well" },
        MaxShelves = 6,
    };
}
