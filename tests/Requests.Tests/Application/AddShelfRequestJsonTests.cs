using System.Text.Json;
using Requests.Application.Shelves;

namespace Requests.Tests.Application;

// Same options ASP.NET Core uses for request bodies.
public class AddShelfRequestJsonTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Valid_body_is_read()
    {
        var request = Read("""{ "productTypeCode": 1, "capacity": 12, "price": 1.5 }""");

        Assert.Equal(1, request.ProductTypeCode);
        Assert.Equal(12, request.Capacity);
        Assert.Equal(1.5, request.Price);
    }

    [Fact]
    public void Whole_number_price_is_read()
    {
        Assert.Equal(2, Read("""{ "price": 2 }""").Price);
    }

    [Fact]
    public void Null_and_missing_numbers_are_read_as_null_for_the_validator()
    {
        var request = Read("""{ "productTypeCode": null, "capacity": null }""");

        Assert.Null(request.ProductTypeCode);
        Assert.Null(request.Capacity);
        Assert.Null(request.Price);
    }

    [Theory]
    [InlineData("""{ "productTypeCode": "1", "capacity": 12, "price": 1.5 }""")]
    [InlineData("""{ "productTypeCode": 1, "capacity": "12", "price": 1.5 }""")]
    [InlineData("""{ "productTypeCode": 1, "capacity": 12, "price": "1.5" }""")]
    [InlineData("""{ "productTypeCode": 1, "capacity": 12, "price": "NaN" }""")]
    [InlineData("""{ "productTypeCode": 1, "capacity": 2.5, "price": 1.5 }""")]
    [InlineData("""{ "productTypeCode": 1.5, "capacity": 12, "price": 1.5 }""")]
    public void Numbers_as_strings_or_decimal_integers_are_rejected(string body)
    {
        Assert.Throws<JsonException>(() => Read(body));
    }

    [Theory]
    [InlineData("""{ "productTypeCode": 1, "capacity": 12, "price": 1.5, "id": "s-1" }""")]
    [InlineData("""{ "productTypeCode": 1, "capacity": 12, "price": 1.5, "createdBy": "x" }""")]
    [InlineData("""{ "productTypeCode": 1, "capacity": 12, "price": 1.5, "categoryCode": 1 }""")]
    [InlineData("""{ "productTypeCode": 1, "capacity": 12, "priceCents": 150 }""")]
    [InlineData("""{ "productType": "water", "capacity": 12, "price": 1.5 }""")]
    public void Unknown_fields_are_rejected(string body)
    {
        Assert.Throws<JsonException>(() => Read(body));
    }

    private static AddShelfRequest Read(string body) => JsonSerializer.Deserialize<AddShelfRequest>(body, WebOptions)!;
}
