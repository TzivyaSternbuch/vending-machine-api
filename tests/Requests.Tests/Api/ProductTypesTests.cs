using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Requests.Tests.Api;

/// <summary>Integration tests for POST and GET /product-types. A new API and database for every test.</summary>
public class ProductTypesTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    private readonly HttpClient _client;

    public ProductTypesTests()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = TestTokens.Admin("admin-42");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Create_returns_201_with_the_new_code_and_no_location()
    {
        var response = await PostAsync("""{ "name": "Lemonade", "categoryCode": 1 }""");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);
        var productType = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(8, productType.GetProperty("code").GetInt32());
        Assert.Equal("lemonade", productType.GetProperty("name").GetString());
        Assert.Equal(1, productType.GetProperty("categoryCode").GetInt32());
    }

    [Fact]
    public async Task GetAll_returns_seeded_and_added_types_ordered_by_code()
    {
        await PostAsync("""{ "name": "lemonade", "categoryCode": 1 }""");

        var response = await _client.GetAsync("/product-types");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var productTypes = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8], productTypes.Select(p => p.GetProperty("code").GetInt32()));
        Assert.Equal("water", productTypes[0].GetProperty("name").GetString());
        Assert.Equal(1, productTypes[0].GetProperty("categoryCode").GetInt32());
        Assert.Equal("lemonade", productTypes[7].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Type_can_use_a_category_added_through_the_api()
    {
        await _client.PostAsync("/product-categories", Json("""{ "name": "fruit" }"""));

        var response = await PostAsync("""{ "name": "apple", "categoryCode": 4 }""");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var productType = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(4, productType.GetProperty("categoryCode").GetInt32());
    }

    [Fact]
    public async Task Taken_name_returns_409()
    {
        var response = await PostAsync("""{ "name": "water", "categoryCode": 1 }""");

        var error = await ErrorAssert.HasErrorAsync(response, HttpStatusCode.Conflict, "Conflict");
        Assert.Equal("Product type 'water' already exists.", error.GetProperty("message").GetString());
    }

    [Theory]
    [InlineData("""{ "name": "lemonade", "categoryCode": 9 }""", "categoryCode", "must be one of: 1, 2, 3")]
    [InlineData("""{ "name": "lemonade" }""", "categoryCode", "is required")]
    [InlineData("""{ "name": "lemonade", "categoryCode": null }""", "categoryCode", "is required")]
    [InlineData("""{ "categoryCode": 1 }""", "name", "is required")]
    public async Task Invalid_type_returns_400_with_field_and_issue(string body, string expectedField, string expectedIssue)
    {
        var response = await PostAsync(body);

        var error = await ErrorAssert.HasErrorAsync(response, HttpStatusCode.BadRequest, "BadRequest");
        var detail = Assert.Single(error.GetProperty("details").EnumerateArray());
        Assert.Equal(expectedField, detail.GetProperty("field").GetString());
        Assert.Equal(expectedIssue, detail.GetProperty("issue").GetString());
    }

    [Theory]
    [InlineData("""{ "name": "lemonade", "categoryCode": "1" }""")]
    [InlineData("""{ "name": "lemonade", "categoryCode": 1.5 }""")]
    [InlineData("""{ "name": "lemonade", "categoryCode": 1, "code": 20 }""")]
    [InlineData("""{ "name": "lemonade", "category": "drink" }""")]
    [InlineData("{ not json")]
    public async Task Wrong_type_unknown_field_or_malformed_body_returns_400(string body)
    {
        var response = await PostAsync(body);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.BadRequest, "BadRequest");
    }

    [Fact]
    public async Task Missing_token_returns_401_on_both_endpoints()
    {
        using var client = _factory.CreateClient();

        var post = await client.PostAsync("/product-types", Json("""{ "name": "lemonade", "categoryCode": 1 }"""));
        var get = await client.GetAsync("/product-types");

        await ErrorAssert.HasErrorAsync(post, HttpStatusCode.Unauthorized, "Unauthorized");
        await ErrorAssert.HasErrorAsync(get, HttpStatusCode.Unauthorized, "Unauthorized");
    }

    [Fact]
    public async Task Non_admin_returns_403_on_both_endpoints()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = TestTokens.Bearer(TestTokens.Create("user-7", "user"));

        var post = await client.PostAsync("/product-types", Json("""{ "name": "lemonade", "categoryCode": 1 }"""));
        var get = await client.GetAsync("/product-types");

        await ErrorAssert.HasErrorAsync(post, HttpStatusCode.Forbidden, "Forbidden");
        await ErrorAssert.HasErrorAsync(get, HttpStatusCode.Forbidden, "Forbidden");
    }

    private Task<HttpResponseMessage> PostAsync(string body) => _client.PostAsync("/product-types", Json(body));

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");
}
