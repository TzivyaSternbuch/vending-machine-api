using Requests.Application.Exceptions;
using Requests.Application.Products;
using Requests.Infrastructure.Products;
using Requests.Tests.Infrastructure;

namespace Requests.Tests.Application;

public class ProductTypeServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    private readonly ProductTypeService _service;

    public ProductTypeServiceTests()
    {
        _service = new ProductTypeService(
            new ProductTypeRepository(_database.CreateContext()),
            new ProductTypeValidator(new ProductCategoryRepository(_database.CreateContext())));
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Create_stores_and_returns_type_with_new_code()
    {
        var response = await _service.CreateAsync(Request("fruit", 2), CancellationToken.None);

        Assert.Equal(8, response.Code);
        Assert.Equal("fruit", response.Name);
        Assert.Equal(2, response.CategoryCode);
        Assert.Contains(await _service.GetAllAsync(CancellationToken.None), p => p.Code == 8 && p.CategoryCode == 2);
    }

    [Fact]
    public async Task Name_is_stored_trimmed_and_lowercased()
    {
        var response = await _service.CreateAsync(Request(" Lemonade ", 1), CancellationToken.None);

        Assert.Equal("lemonade", response.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ice cream")]
    public async Task Invalid_name_is_rejected(string? name)
    {
        await AssertRejectedAsync(Request(name, 1), "name");
    }

    [Fact]
    public async Task Missing_categoryCode_is_rejected()
    {
        await AssertRejectedAsync(Request("fruit", null), "categoryCode");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public async Task Unknown_categoryCode_is_rejected_listing_the_allowed_codes(int categoryCode)
    {
        var exception = await AssertRejectedAsync(Request("fruit", categoryCode), "categoryCode");

        Assert.Equal("must be one of: 1, 2, 3", exception.Issue);
    }

    [Fact]
    public async Task Name_is_checked_before_categoryCode()
    {
        await AssertRejectedAsync(Request("", 99), "name");
    }

    [Theory]
    [InlineData("water")]
    [InlineData("WATER")]
    public async Task Taken_name_is_a_conflict(string name)
    {
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(Request(name, 1), CancellationToken.None));

        Assert.Equal("Product type 'water' already exists.", exception.Message);
    }

    [Fact]
    public async Task Invalid_category_is_reported_before_a_taken_name()
    {
        await AssertRejectedAsync(Request("water", 99), "categoryCode");
    }

    [Fact]
    public async Task Type_can_use_a_category_added_later()
    {
        var categories = new ProductCategoryService(
            new ProductCategoryRepository(_database.CreateContext()),
            new ProductCategoryValidator());
        var fruit = await categories.CreateAsync(new CreateProductCategoryRequest { Name = "fruit" }, CancellationToken.None);

        var response = await _service.CreateAsync(Request("apple", fruit.Code), CancellationToken.None);

        Assert.Equal(fruit.Code, response.CategoryCode);
    }

    private async Task<ValidationException> AssertRejectedAsync(CreateProductTypeRequest request, string expectedField)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(request, CancellationToken.None));

        Assert.Equal(expectedField, exception.Field);
        return exception;
    }

    private static CreateProductTypeRequest Request(string? name, int? categoryCode) =>
        new() { Name = name, CategoryCode = categoryCode };
}
