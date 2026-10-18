using Requests.Application.Exceptions;
using Requests.Application.Shelves;

namespace Requests.Tests.Application;

public class LoadInventoryRequestValidatorTests
{
    private readonly LoadInventoryRequestValidator _validator = new(new ShelfSettings());

    [Fact]
    public void Valid_request_passes()
    {
        _validator.Validate(Request(1, 8));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_or_blank_shelfId_is_rejected(string? shelfId)
    {
        var exception = AssertRejected(Request(1, 8, shelfId), "shelfId");

        Assert.Equal("is required", exception.Issue);
    }

    [Fact]
    public void Missing_productTypeCode_is_rejected()
    {
        var exception = AssertRejected(Request(null, 8), "productTypeCode");

        Assert.Equal("is required", exception.Issue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(999)]
    public void Any_productTypeCode_passes_because_the_service_compares_it_to_the_shelf(int productTypeCode)
    {
        _validator.Validate(Request(productTypeCode, 8));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void Quantity_from_1_to_the_limit_is_accepted(int quantity)
    {
        _validator.Validate(Request(1, quantity));
    }

    [Theory]
    [InlineData(null, "is required")]
    [InlineData(0, "must be between 1 and 50")]
    [InlineData(-1, "must be between 1 and 50")]
    [InlineData(51, "must be between 1 and 50")]
    public void Invalid_quantity_is_rejected(int? quantity, string expectedIssue)
    {
        var exception = AssertRejected(Request(1, quantity), "quantity");

        Assert.Equal(expectedIssue, exception.Issue);
    }

    [Fact]
    public void Quantity_limit_comes_from_settings()
    {
        var validator = new LoadInventoryRequestValidator(new ShelfSettings { MaxShelfCapacity = 10 });

        var exception = Assert.Throws<ValidationException>(() => validator.Validate(Request(1, 11)));

        Assert.Equal("must be between 1 and 10", exception.Issue);
    }

    [Fact]
    public void Rules_are_checked_in_field_order()
    {
        AssertRejected(Request(null, null, null), "shelfId");
        AssertRejected(Request(null, null), "productTypeCode");
        AssertRejected(Request(1, null), "quantity");
    }

    private ValidationException AssertRejected(LoadInventoryRequest request, string expectedField)
    {
        var exception = Assert.Throws<ValidationException>(() => _validator.Validate(request));

        Assert.Equal(expectedField, exception.Field);
        return exception;
    }

    private static LoadInventoryRequest Request(int? productTypeCode, int? quantity, string? shelfId = "s-1") => new()
    {
        ShelfId = shelfId,
        ProductTypeCode = productTypeCode,
        Quantity = quantity,
    };
}
