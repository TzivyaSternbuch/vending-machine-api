using Requests.Application.Exceptions;
using Requests.Application.Products;
using Requests.Infrastructure.Products;
using Requests.Tests.Infrastructure;

namespace Requests.Tests.Application;

public class ProductCategoryServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    private readonly ProductCategoryService _service;

    public ProductCategoryServiceTests()
    {
        _service = new ProductCategoryService(
            new ProductCategoryRepository(_database.CreateContext()),
            new ProductCategoryValidator());
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Create_stores_and_returns_category_with_new_code()
    {
        var response = await _service.CreateAsync(Request("fruit"), CancellationToken.None);

        Assert.Equal(4, response.Code);
        Assert.Equal("fruit", response.Name);
        Assert.Contains(await _service.GetAllAsync(CancellationToken.None), c => c.Code == 4 && c.Name == "fruit");
    }

    [Theory]
    [InlineData("  Fruit ", "fruit")]
    [InlineData("ICE-CREAM", "ice-cream")]
    [InlineData("snack2", "snack2")]
    public async Task Name_is_stored_trimmed_and_lowercased(string name, string expected)
    {
        var response = await _service.CreateAsync(Request(name), CancellationToken.None);

        Assert.Equal(expected, response.Name);
    }

    [Fact]
    public async Task Name_of_32_characters_is_accepted()
    {
        var response = await _service.CreateAsync(Request(new string('a', 32)), CancellationToken.None);

        Assert.Equal(32, response.Name.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ice cream")]
    [InlineData("ice_cream")]
    [InlineData("café")]
    public async Task Invalid_name_is_rejected(string? name)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(Request(name), CancellationToken.None));

        Assert.Equal("name", exception.Field);
    }

    [Fact]
    public async Task Name_of_33_characters_is_rejected()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(Request(new string('a', 33)), CancellationToken.None));

        Assert.Equal("name", exception.Field);
    }

    [Theory]
    [InlineData("drink")]
    [InlineData(" Drink ")]
    public async Task Taken_name_is_a_conflict(string name)
    {
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(Request(name), CancellationToken.None));

        Assert.Equal("Product category 'drink' already exists.", exception.Message);
    }

    [Fact]
    public async Task GetAll_returns_seeded_categories_ordered_by_code()
    {
        var all = await _service.GetAllAsync(CancellationToken.None);

        Assert.Equal(["drink", "snack", "candy"], all.Select(c => c.Name));
    }

    private static CreateProductCategoryRequest Request(string? name) => new() { Name = name };
}
