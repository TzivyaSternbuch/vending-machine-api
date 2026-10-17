using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Requests.Tests.Infrastructure;

namespace Requests.Tests.Api;

// Integration tests for POST /machines/{id}/shelves. A new API and database for every test.
public class AddShelfTests : IDisposable
{
    private const string VALID_SHELF = """{ "productTypeCode": 1, "capacity": 12, "price": 1.5 }""";

    private readonly ApiFactory _factory = new();

    private readonly HttpClient _client;

    public AddShelfTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Add_returns_201_with_the_shelf_and_the_machine_shows_it()
    {
        var machineId = await CreateMachineAsync(_client, maxShelves: 6);

        var response = await AddShelfAsync(_client, machineId, VALID_SHELF);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);
        var shelf = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(shelf.GetProperty("id").GetString()));
        Assert.Equal(1, shelf.GetProperty("productTypeCode").GetInt32());
        Assert.Equal(12, shelf.GetProperty("capacity").GetInt32());
        Assert.Equal(1.5, shelf.GetProperty("price").GetDouble());

        var machine = await GetMachineAsync(_client, machineId);
        var listed = Assert.Single(machine.GetProperty("shelves").EnumerateArray());
        Assert.Equal(shelf.GetRawText(), listed.GetRawText());
        Assert.Equal("admin-7", machine.GetProperty("createdBy").GetString());
        Assert.Equal("admin-42", machine.GetProperty("updatedBy").GetString());
        Assert.True(
            machine.GetProperty("updatedAt").GetDateTime() > machine.GetProperty("createdAt").GetDateTime(),
            "Adding a shelf must move the machine's updatedAt forward.");
    }

    [Fact]
    public async Task Adding_up_to_maxShelves_succeeds_and_shelves_are_listed_in_order()
    {
        var machineId = await CreateMachineAsync(_client, maxShelves: 3);

        var ids = new List<string?>();
        foreach (var productTypeCode in new[] { 1, 4, 6 })
        {
            var response = await AddShelfAsync(_client, machineId, Shelf(productTypeCode));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            ids.Add((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString());
        }

        var shelves = (await GetMachineAsync(_client, machineId)).GetProperty("shelves").EnumerateArray().ToList();
        Assert.Equal(ids, shelves.Select(s => s.GetProperty("id").GetString()));
        Assert.Equal([1, 4, 6], shelves.Select(s => s.GetProperty("productTypeCode").GetInt32()));
    }

    [Fact]
    public async Task Adding_past_maxShelves_returns_409_and_stores_nothing()
    {
        var machineId = await CreateMachineAsync(_client, maxShelves: 3);
        for (var i = 0; i < 3; i++)
        {
            await AddShelfAsync(_client, machineId, VALID_SHELF);
        }

        var response = await AddShelfAsync(_client, machineId, VALID_SHELF);

        var error = await ErrorAssert.HasErrorAsync(response, HttpStatusCode.Conflict, "Conflict");
        Assert.Equal("Machine already has its maximum of 3 shelves.", error.GetProperty("message").GetString());
        Assert.Equal(3, (await GetMachineAsync(_client, machineId)).GetProperty("shelves").GetArrayLength());
    }

    [Fact]
    public async Task Machine_created_without_maxShelves_takes_20_shelves()
    {
        var machineId = await CreateMachineAsync(_client, maxShelves: null);
        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(HttpStatusCode.Created, (await AddShelfAsync(_client, machineId, VALID_SHELF)).StatusCode);
        }

        var response = await AddShelfAsync(_client, machineId, VALID_SHELF);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.Conflict, "Conflict");
    }

    [Fact]
    public async Task Limit_of_1_takes_one_shelf()
    {
        var machineId = await CreateMachineAsync(_client, maxShelves: 1);

        var first = await AddShelfAsync(_client, machineId, VALID_SHELF);
        var second = await AddShelfAsync(_client, machineId, VALID_SHELF);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        await ErrorAssert.HasErrorAsync(second, HttpStatusCode.Conflict, "Conflict");
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("not-a-guid-%21")]
    public async Task Unknown_or_malformed_machine_id_returns_404(string machineId)
    {
        var response = await AddShelfAsync(_client, machineId, VALID_SHELF);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.NotFound, "NotFound");
    }

    [Theory]
    [InlineData("""{ "productTypeCode": 99, "capacity": 12, "price": 1.5 }""", "productTypeCode", "must be one of: 1, 2, 3, 4, 5, 6, 7")]
    [InlineData("""{ "capacity": 12, "price": 1.5 }""", "productTypeCode", "is required")]
    [InlineData("""{ "productTypeCode": null, "capacity": 12, "price": 1.5 }""", "productTypeCode", "is required")]
    [InlineData("""{ "productTypeCode": 1, "price": 1.5 }""", "capacity", "is required")]
    [InlineData("""{ "productTypeCode": 1, "capacity": 51, "price": 1.5 }""", "capacity", "must be at most 50")]
    [InlineData("""{ "productTypeCode": 1, "capacity": 12 }""", "price", "is required")]
    [InlineData("""{ "productTypeCode": 1, "capacity": 12, "price": 0 }""", "price", "must be greater than 0")]
    [InlineData("""{ "productTypeCode": 1, "capacity": 12, "price": -1.5 }""", "price", "must be greater than 0")]
    public async Task Invalid_shelf_returns_400_with_field_and_issue(string body, string expectedField, string expectedIssue)
    {
        var machineId = await CreateMachineAsync(_client, maxShelves: 6);

        var response = await AddShelfAsync(_client, machineId, body);

        var error = await ErrorAssert.HasErrorAsync(response, HttpStatusCode.BadRequest, "BadRequest");
        var detail = Assert.Single(error.GetProperty("details").EnumerateArray());
        Assert.Equal(expectedField, detail.GetProperty("field").GetString());
        Assert.Equal(expectedIssue, detail.GetProperty("issue").GetString());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public async Task Capacity_up_to_50_is_accepted(int capacity)
    {
        var machineId = await CreateMachineAsync(_client, maxShelves: 6);

        var response = await AddShelfAsync(_client, machineId, Shelf(capacity: capacity));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Type_added_through_the_api_is_accepted()
    {
        var machineId = await CreateMachineAsync(_client, maxShelves: 6);
        await SendAsync(_client, HttpMethod.Post, "/product-types", """{ "name": "lemonade", "categoryCode": 1 }""");

        var response = await AddShelfAsync(_client, machineId, Shelf(productTypeCode: 8));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("""{ "productTypeCode": "1", "capacity": 12, "price": 1.5 }""")]
    [InlineData("""{ "productTypeCode": 1, "capacity": "5", "price": 1.5 }""")]
    [InlineData("""{ "productTypeCode": 1, "capacity": 2.5, "price": 1.5 }""")]
    [InlineData("""{ "productTypeCode": 1, "capacity": 12, "price": "1.5" }""")]
    [InlineData("""{ "productTypeCode": 1, "capacity": 12, "price": 1.5, "id": "s-1" }""")]
    [InlineData("""{ "productTypeCode": 1, "capacity": 12, "price": 1.5, "createdBy": "someone" }""")]
    [InlineData("""{ "productTypeCode": 1, "capacity": 12, "price": 1.5, "categoryCode": 1 }""")]
    [InlineData("""{ "productTypeCode": 1, "capacity": 12, "price": 1.5, "color": "red" }""")]
    [InlineData("{ not json")]
    [InlineData("")]
    public async Task Wrong_type_unknown_field_or_malformed_body_returns_400(string body)
    {
        var machineId = await CreateMachineAsync(_client, maxShelves: 6);

        var response = await AddShelfAsync(_client, machineId, body);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.BadRequest, "BadRequest");
    }

    [Fact]
    public async Task Invalid_body_on_a_full_machine_returns_400()
    {
        var machineId = await CreateMachineAsync(_client, maxShelves: 1);
        await AddShelfAsync(_client, machineId, VALID_SHELF);

        var response = await AddShelfAsync(_client, machineId, Shelf(price: 0));

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.BadRequest, "BadRequest");
    }

    [Fact]
    public async Task Invalid_body_on_an_unknown_machine_returns_400()
    {
        var response = await AddShelfAsync(_client, "missing", Shelf(productTypeCode: 99));

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.BadRequest, "BadRequest");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "admin")]
    [InlineData("", "admin")]
    public async Task Missing_token_or_user_id_returns_401_before_the_body_is_read(string? userId, string? role)
    {
        var response = await SendAsync(_client, HttpMethod.Post, "/machines/any-id/shelves", "{ not json", userId, role);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.Unauthorized, "Unauthorized");
    }

    [Theory]
    [InlineData("user")]
    [InlineData("Admin")]
    [InlineData(null)]
    public async Task Non_admin_returns_403_before_the_body_is_read(string? role)
    {
        var response = await SendAsync(_client, HttpMethod.Post, "/machines/any-id/shelves", "{ not json", "user-7", role);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.Forbidden, "Forbidden");
    }

    [Fact]
    public async Task Shelf_is_still_there_after_a_restart()
    {
        using var database = new TestDatabase();
        string machineId;
        string? shelfId;

        using (var first = ApiFactory.OnDatabase(database))
        using (var client = first.CreateClient())
        {
            machineId = await CreateMachineAsync(client, maxShelves: 6);
            var added = await AddShelfAsync(client, machineId, VALID_SHELF);
            shelfId = (await added.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        }

        using var second = ApiFactory.OnDatabase(database);
        using var restarted = second.CreateClient();
        var machine = await GetMachineAsync(restarted, machineId);

        var shelf = Assert.Single(machine.GetProperty("shelves").EnumerateArray());
        Assert.Equal(shelfId, shelf.GetProperty("id").GetString());
        Assert.Equal(1.5, shelf.GetProperty("price").GetDouble());
    }

    private static async Task<string> CreateMachineAsync(HttpClient client, int? maxShelves)
    {
        var maxShelvesField = maxShelves is null ? "" : $""", "maxShelves": {maxShelves}""";
        var body = $$"""{ "name": "Riverside Snacks", "location": { "region": "Riverside Market", "zipCode": "10001" }{{maxShelvesField}} }""";

        var response = await SendAsync(client, HttpMethod.Post, "/machines", body, userId: "admin-7");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> GetMachineAsync(HttpClient client, string machineId)
    {
        var response = await SendAsync(client, HttpMethod.Get, $"/machines/{machineId}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static Task<HttpResponseMessage> AddShelfAsync(HttpClient client, string machineId, string body) =>
        SendAsync(client, HttpMethod.Post, $"/machines/{machineId}/shelves", body);

    private static Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        string? body,
        string? userId = "admin-42",
        string? role = "admin")
    {
        var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        if (userId is not null || role is not null)
        {
            request.Headers.Authorization = TestTokens.Bearer(TestTokens.Create(userId, role));
        }

        return client.SendAsync(request);
    }

    private static string Shelf(int productTypeCode = 1, int capacity = 12, double price = 1.5) =>
        $$"""{ "productTypeCode": {{productTypeCode}}, "capacity": {{capacity}}, "price": {{price.ToString(System.Globalization.CultureInfo.InvariantCulture)}} }""";
}
