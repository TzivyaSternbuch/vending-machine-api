using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Requests.Tests.Api;

// Integration tests for POST /machines/{id}/inventory. A new API and database for every test.
// Unless stated, the shelf is type 1 (water) with capacity 10.
public class LoadInventoryTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    private readonly HttpClient _client;

    public LoadInventoryTests()
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
    public async Task Load_returns_200_with_the_shelf_and_the_machine_shows_it()
    {
        var (machineId, shelfId) = await CreateMachineWithShelfAsync();

        var response = await LoadAsync(machineId, Load(shelfId, quantity: 8));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var shelf = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(shelfId, shelf.GetProperty("id").GetString());
        Assert.Equal(1, shelf.GetProperty("productTypeCode").GetInt32());
        Assert.Equal(10, shelf.GetProperty("capacity").GetInt32());
        Assert.Equal(8, shelf.GetProperty("quantity").GetInt32());

        var machine = await GetMachineAsync(machineId);
        var listed = Assert.Single(machine.GetProperty("shelves").EnumerateArray());
        Assert.Equal(shelf.GetRawText(), listed.GetRawText());
        Assert.Equal("admin-42", machine.GetProperty("updatedBy").GetString());
        Assert.True(
            machine.GetProperty("updatedAt").GetDateTime() > machine.GetProperty("createdAt").GetDateTime(),
            "Loading must move the machine's updatedAt forward.");
    }

    [Fact]
    public async Task New_shelf_shows_quantity_0()
    {
        var (machineId, _) = await CreateMachineWithShelfAsync();

        var shelf = Assert.Single((await GetMachineAsync(machineId)).GetProperty("shelves").EnumerateArray());

        Assert.Equal(0, shelf.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task Loads_add_up_and_can_fill_the_shelf_exactly()
    {
        var (machineId, shelfId) = await CreateMachineWithShelfAsync();
        await LoadAsync(machineId, Load(shelfId, quantity: 8));

        var response = await LoadAsync(machineId, Load(shelfId, quantity: 2));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(10, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task Wrong_product_type_returns_400_on_productTypeCode_and_changes_nothing()
    {
        var (machineId, shelfId) = await CreateMachineWithShelfAsync();

        var response = await LoadAsync(machineId, Load(shelfId, productTypeCode: 2));

        var error = await ErrorAssert.HasErrorAsync(response, HttpStatusCode.BadRequest, "BadRequest");
        var detail = Assert.Single(error.GetProperty("details").EnumerateArray());
        Assert.Equal("productTypeCode", detail.GetProperty("field").GetString());
        Assert.Equal("must be 1, the shelf's product type", detail.GetProperty("issue").GetString());
        Assert.Equal(0, await GetQuantityAsync(machineId));
    }

    [Fact]
    public async Task Load_past_capacity_returns_409_with_the_free_space_and_changes_nothing()
    {
        var (machineId, shelfId) = await CreateMachineWithShelfAsync();
        await LoadAsync(machineId, Load(shelfId, quantity: 8));

        var response = await LoadAsync(machineId, Load(shelfId, quantity: 3));

        var error = await ErrorAssert.HasErrorAsync(response, HttpStatusCode.Conflict, "Conflict");
        Assert.Equal("Shelf has room for 2 more items; 3 requested.", error.GetProperty("message").GetString());
        Assert.Equal(8, await GetQuantityAsync(machineId));
    }

    [Fact]
    public async Task Load_larger_than_an_empty_shelf_returns_409()
    {
        var (machineId, shelfId) = await CreateMachineWithShelfAsync();

        var response = await LoadAsync(machineId, Load(shelfId, quantity: 11));

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.Conflict, "Conflict");
    }

    [Fact]
    public async Task Unknown_machine_returns_404()
    {
        var response = await LoadAsync("missing", Load("any-shelf"));

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.NotFound, "NotFound");
    }

    [Fact]
    public async Task Unknown_shelf_returns_404()
    {
        var (machineId, _) = await CreateMachineWithShelfAsync();

        var response = await LoadAsync(machineId, Load("missing"));

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.NotFound, "NotFound");
    }

    [Fact]
    public async Task Shelf_of_another_machine_returns_404()
    {
        var (machineId, _) = await CreateMachineWithShelfAsync();
        var (_, otherShelfId) = await CreateMachineWithShelfAsync();

        var response = await LoadAsync(machineId, Load(otherShelfId));

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.NotFound, "NotFound");
    }

    [Theory]
    [InlineData("""{ "productTypeCode": 1, "quantity": 5 }""", "shelfId", "is required")]
    [InlineData("""{ "shelfId": "", "productTypeCode": 1, "quantity": 5 }""", "shelfId", "is required")]
    [InlineData("""{ "shelfId": "s-1", "quantity": 5 }""", "productTypeCode", "is required")]
    [InlineData("""{ "shelfId": "s-1", "productTypeCode": null, "quantity": 5 }""", "productTypeCode", "is required")]
    [InlineData("""{ "shelfId": "s-1", "productTypeCode": 1 }""", "quantity", "is required")]
    [InlineData("""{ "shelfId": "s-1", "productTypeCode": 1, "quantity": 0 }""", "quantity", "must be between 1 and 50")]
    [InlineData("""{ "shelfId": "s-1", "productTypeCode": 1, "quantity": -1 }""", "quantity", "must be between 1 and 50")]
    [InlineData("""{ "shelfId": "s-1", "productTypeCode": 1, "quantity": 51 }""", "quantity", "must be between 1 and 50")]
    public async Task Invalid_field_returns_400_naming_the_field(string body, string expectedField, string expectedIssue)
    {
        var (machineId, _) = await CreateMachineWithShelfAsync();

        var response = await LoadAsync(machineId, body);

        var error = await ErrorAssert.HasErrorAsync(response, HttpStatusCode.BadRequest, "BadRequest");
        var detail = Assert.Single(error.GetProperty("details").EnumerateArray());
        Assert.Equal(expectedField, detail.GetProperty("field").GetString());
        Assert.Equal(expectedIssue, detail.GetProperty("issue").GetString());
    }

    [Theory]
    [InlineData("""{ "shelfId": "s-1", "productTypeCode": 1, "quantity": 2.5 }""")]
    [InlineData("""{ "shelfId": "s-1", "productTypeCode": 1, "quantity": 5, "capacity": 10 }""")]
    [InlineData("""{ "shelfId": 7, "productTypeCode": 1, "quantity": 5 }""")]
    [InlineData("{ not json")]
    [InlineData("")]
    public async Task Decimal_unknown_field_or_malformed_body_returns_400(string body)
    {
        var (machineId, _) = await CreateMachineWithShelfAsync();

        var response = await LoadAsync(machineId, body);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.BadRequest, "BadRequest");
    }

    [Fact]
    public async Task Invalid_body_on_an_unknown_machine_returns_400()
    {
        var response = await LoadAsync("missing", Load("any-shelf", quantity: 0));

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.BadRequest, "BadRequest");
    }

    [Fact]
    public async Task Wrong_product_type_on_a_full_shelf_returns_400_not_409()
    {
        var (machineId, shelfId) = await CreateMachineWithShelfAsync();
        await LoadAsync(machineId, Load(shelfId, quantity: 10));

        var response = await LoadAsync(machineId, Load(shelfId, productTypeCode: 2));

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.BadRequest, "BadRequest");
    }

    [Fact]
    public async Task Concurrent_loads_give_one_200_and_never_overfill()
    {
        var (machineId, shelfId) = await CreateMachineWithShelfAsync();
        await LoadAsync(machineId, Load(shelfId, quantity: 8));

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => LoadAsync(machineId, Load(shelfId, quantity: 2))));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(9, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(10, await GetQuantityAsync(machineId));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "admin")]
    [InlineData("", "admin")]
    public async Task Missing_token_or_user_id_returns_401_before_the_body_is_read(string? userId, string? role)
    {
        var response = await SendAsync(HttpMethod.Post, "/machines/any-id/inventory", "{ not json", userId, role);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.Unauthorized, "Unauthorized");
    }

    [Theory]
    [InlineData("user")]
    [InlineData("Admin")]
    [InlineData(null)]
    public async Task Non_admin_returns_403_before_the_body_is_read(string? role)
    {
        var response = await SendAsync(HttpMethod.Post, "/machines/any-id/inventory", "{ not json", "user-7", role);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.Forbidden, "Forbidden");
    }

    private async Task<(string MachineId, string ShelfId)> CreateMachineWithShelfAsync()
    {
        const string MACHINE = """{ "name": "Riverside Snacks", "location": { "region": "Riverside Market", "zipCode": "10001" } }""";
        var machine = await SendAsync(HttpMethod.Post, "/machines", MACHINE, userId: "admin-7");
        Assert.Equal(HttpStatusCode.Created, machine.StatusCode);
        var machineId = (await machine.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

        const string SHELF = """{ "productTypeCode": 1, "capacity": 10, "price": 4.5 }""";
        var shelf = await SendAsync(HttpMethod.Post, $"/machines/{machineId}/shelves", SHELF, userId: "admin-7");
        Assert.Equal(HttpStatusCode.Created, shelf.StatusCode);
        var shelfId = (await shelf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

        return (machineId, shelfId);
    }

    private async Task<JsonElement> GetMachineAsync(string machineId)
    {
        var response = await SendAsync(HttpMethod.Get, $"/machines/{machineId}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<int> GetQuantityAsync(string machineId)
    {
        var shelf = Assert.Single((await GetMachineAsync(machineId)).GetProperty("shelves").EnumerateArray());
        return shelf.GetProperty("quantity").GetInt32();
    }

    private Task<HttpResponseMessage> LoadAsync(string machineId, string body) =>
        SendAsync(HttpMethod.Post, $"/machines/{machineId}/inventory", body);

    private Task<HttpResponseMessage> SendAsync(
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

        return _client.SendAsync(request);
    }

    private static string Load(string shelfId, int productTypeCode = 1, int quantity = 5) =>
        $$"""{ "shelfId": "{{shelfId}}", "productTypeCode": {{productTypeCode}}, "quantity": {{quantity}} }""";
}
