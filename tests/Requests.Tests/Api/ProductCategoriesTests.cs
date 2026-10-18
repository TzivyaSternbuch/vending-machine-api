using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Requests.Tests.Api;

/// <summary>Integration tests for POST and GET /product-categories. A new API and database for every test.</summary>
public class ProductCategoriesTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    private readonly HttpClient _client;

    public ProductCategoriesTests()
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
        var response = await PostAsync("""{ "name": " Fruit " }""");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);
        var category = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(4, category.GetProperty("code").GetInt32());
        Assert.Equal("fruit", category.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetAll_returns_seeded_and_added_categories_ordered_by_code()
    {
        await PostAsync("""{ "name": "fruit" }""");

        var response = await _client.GetAsync("/product-categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            ["1:drink", "2:snack", "3:candy", "4:fruit"],
            categories.EnumerateArray().Select(c => $"{c.GetProperty("code").GetInt32()}:{c.GetProperty("name").GetString()}"));
    }

    [Theory]
    [InlineData("drink")]
    [InlineData("DRINK")]
    public async Task Taken_name_returns_409(string name)
    {
        var response = await PostAsync($$"""{ "name": "{{name}}" }""");

        var error = await ErrorAssert.HasErrorAsync(response, HttpStatusCode.Conflict, "Conflict");
        Assert.Equal("Product category 'drink' already exists.", error.GetProperty("message").GetString());
    }

    [Theory]
    [InlineData("""{ "name": "" }""", "is required")]
    [InlineData("""{ }""", "is required")]
    [InlineData("""{ "name": "ice cream" }""", "must contain only letters, digits and '-'")]
    [InlineData("""{ "name": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" }""", "must be at most 32 characters")]
    public async Task Invalid_name_returns_400_with_field_and_issue(string body, string expectedIssue)
    {
        var response = await PostAsync(body);

        var error = await ErrorAssert.HasErrorAsync(response, HttpStatusCode.BadRequest, "BadRequest");
        var detail = Assert.Single(error.GetProperty("details").EnumerateArray());
        Assert.Equal("name", detail.GetProperty("field").GetString());
        Assert.Equal(expectedIssue, detail.GetProperty("issue").GetString());
    }

    [Theory]
    [InlineData("""{ "name": "fruit", "code": 9 }""")]
    [InlineData("""{ "name": "fruit", "color": "red" }""")]
    [InlineData("""{ "name": 5 }""")]
    [InlineData("{ not json")]
    public async Task Unknown_field_wrong_type_or_malformed_body_returns_400(string body)
    {
        var response = await PostAsync(body);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.BadRequest, "BadRequest");
    }

    [Fact]
    public async Task Missing_token_returns_401_on_both_endpoints()
    {
        using var client = _factory.CreateClient();

        var post = await client.PostAsync("/product-categories", Json("""{ "name": "fruit" }"""));
        var get = await client.GetAsync("/product-categories");

        await ErrorAssert.HasErrorAsync(post, HttpStatusCode.Unauthorized, "Unauthorized");
        await ErrorAssert.HasErrorAsync(get, HttpStatusCode.Unauthorized, "Unauthorized");
    }

    [Fact]
    public async Task Non_admin_returns_403_on_both_endpoints()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = TestTokens.Bearer(TestTokens.Create("user-7", "user"));

        var post = await client.PostAsync("/product-categories", Json("""{ "name": "fruit" }"""));
        var get = await client.GetAsync("/product-categories");

        await ErrorAssert.HasErrorAsync(post, HttpStatusCode.Forbidden, "Forbidden");
        await ErrorAssert.HasErrorAsync(get, HttpStatusCode.Forbidden, "Forbidden");
    }

    private Task<HttpResponseMessage> PostAsync(string body) => _client.PostAsync("/product-categories", Json(body));

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");
}
