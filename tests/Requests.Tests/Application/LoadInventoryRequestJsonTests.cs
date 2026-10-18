using System.Text.Json;
using Requests.Application.Shelves;

namespace Requests.Tests.Application;

// Same options ASP.NET Core uses for request bodies.
public class LoadInventoryRequestJsonTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Valid_body_is_read()
    {
        var request = Read("""{ "shelfId": "s-1", "productTypeCode": 1, "quantity": 8 }""");

        Assert.Equal("s-1", request.ShelfId);
        Assert.Equal(1, request.ProductTypeCode);
        Assert.Equal(8, request.Quantity);
    }

    [Fact]
    public void Null_and_missing_values_are_read_as_null_for_the_validator()
    {
        var request = Read("""{ "productTypeCode": null }""");

        Assert.Null(request.ShelfId);
        Assert.Null(request.ProductTypeCode);
        Assert.Null(request.Quantity);
    }

    [Theory]
    [InlineData("""{ "shelfId": "s-1", "productTypeCode": 1, "quantity": 2.5 }""")]
    [InlineData("""{ "shelfId": "s-1", "productTypeCode": 1.5, "quantity": 8 }""")]
    [InlineData("""{ "shelfId": 1, "productTypeCode": 1, "quantity": 8 }""")]
    public void Decimals_or_a_numeric_shelfId_are_rejected(string body)
    {
        Assert.Throws<JsonException>(() => Read(body));
    }

    [Theory]
    [InlineData("""{ "shelfId": "s-1", "productTypeCode": 1, "quantity": 8, "capacity": 10 }""")]
    [InlineData("""{ "shelfId": "s-1", "productCode": 1, "quantity": 8 }""")]
    [InlineData("""{ "shelfId": "s-1", "productTypeCode": 1, "amount": 8 }""")]
    [InlineData("""{ "id": "s-1", "productTypeCode": 1, "quantity": 8 }""")]
    public void Unknown_fields_are_rejected(string body)
    {
        Assert.Throws<JsonException>(() => Read(body));
    }

    private static LoadInventoryRequest Read(string body) =>
        JsonSerializer.Deserialize<LoadInventoryRequest>(body, WebOptions)!;
}
