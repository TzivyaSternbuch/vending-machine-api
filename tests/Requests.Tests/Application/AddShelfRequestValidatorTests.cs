using Requests.Application.Exceptions;
using Requests.Application.Shelves;
using Requests.Domain.Products;
using Requests.Infrastructure.Products;
using Requests.Tests.Infrastructure;

namespace Requests.Tests.Application;

public class AddShelfRequestValidatorTests : IDisposable
{
    private readonly TestDatabase _database = new();

    private readonly AddShelfRequestValidator _validator;

    public AddShelfRequestValidatorTests()
    {
        _validator = NewValidator(new ShelfSettings());
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Valid_request_passes()
    {
        await _validator.ValidateAsync(Request(1, 12, 1.5), CancellationToken.None);
    }

    [Fact]
    public async Task Missing_productTypeCode_is_rejected()
    {
        var exception = await AssertRejectedAsync(Request(null, 12, 1.5), "productTypeCode");

        Assert.Equal("is required", exception.Issue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(-1)]
    public async Task Unknown_productTypeCode_is_rejected_listing_the_allowed_codes(int productTypeCode)
    {
        var exception = await AssertRejectedAsync(Request(productTypeCode, 12, 1.5), "productTypeCode");

        Assert.Equal("must be one of: 1, 2, 3, 4, 5, 6, 7", exception.Issue);
    }

    [Fact]
    public async Task Type_added_later_is_accepted()
    {
        await new ProductTypeRepository(_database.CreateContext())
            .AddAsync(new ProductType { Name = "fruit", CategoryCode = 2 }, CancellationToken.None);

        await _validator.ValidateAsync(Request(8, 12, 1.5), CancellationToken.None);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(0)]
    public async Task Capacity_from_0_to_the_limit_is_accepted(int capacity)
    {
        await _validator.ValidateAsync(Request(1, capacity, 1.5), CancellationToken.None);
    }

    [Theory]
    [InlineData(null, "is required")]
    [InlineData(-1, "must be at least 0")]
    [InlineData(51, "must be at most 50")]
    public async Task Invalid_capacity_is_rejected(int? capacity, string expectedIssue)
    {
        var exception = await AssertRejectedAsync(Request(1, capacity, 1.5), "capacity");

        Assert.Equal(expectedIssue, exception.Issue);
    }

    [Fact]
    public async Task Capacity_limit_comes_from_settings()
    {
        var validator = NewValidator(new ShelfSettings { MaxShelfCapacity = 10 });

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => validator.ValidateAsync(Request(1, 11, 1.5), CancellationToken.None));

        Assert.Equal("must be at most 10", exception.Issue);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1.5)]
    [InlineData(10000)]
    public async Task Price_greater_than_0_is_accepted_with_no_upper_limit(double price)
    {
        await _validator.ValidateAsync(Request(1, 12, price), CancellationToken.None);
    }

    [Theory]
    [InlineData(null, "is required")]
    [InlineData(0.0, "must be greater than 0")]
    [InlineData(-1.5, "must be greater than 0")]
    public async Task Invalid_price_is_rejected(double? price, string expectedIssue)
    {
        var exception = await AssertRejectedAsync(Request(1, 12, price), "price");

        Assert.Equal(expectedIssue, exception.Issue);
    }

    [Fact]
    public async Task Rules_are_checked_in_field_order()
    {
        await AssertRejectedAsync(Request(null, null, null), "productTypeCode");
        await AssertRejectedAsync(Request(1, null, null), "capacity");
        await AssertRejectedAsync(Request(1, 12, null), "price");
    }

    private AddShelfRequestValidator NewValidator(ShelfSettings settings) =>
        new(new ProductTypeRepository(_database.CreateContext()), settings);

    private async Task<ValidationException> AssertRejectedAsync(AddShelfRequest request, string expectedField)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _validator.ValidateAsync(request, CancellationToken.None));

        Assert.Equal(expectedField, exception.Field);
        return exception;
    }

    private static AddShelfRequest Request(int? productTypeCode, int? capacity, double? price) => new()
    {
        ProductTypeCode = productTypeCode,
        Capacity = capacity,
        Price = price,
    };
}
