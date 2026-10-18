using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Requests.Tests.Api;

// Integration tests for POST /machines/{machineId}/shelves/{shelfId}/purchase. A new API and database for every test.
// Unless stated, the shelf is type 1 (water) with capacity 10 and price 4.5.
public class PurchaseTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    private readonly HttpClient _client;

    public PurchaseTests()
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
    public async Task Purchase_with_a_user_token_returns_200_with_the_item_and_lowers_the_quantity()
    {
        var (machineId, shelfId) = await CreateMachineWithShelfAsync(quantity: 5);

        var response = await PurchaseAsync(machineId, shelfId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var purchase = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(machineId, purchase.GetProperty("machineId").GetString());
        Assert.Equal(shelfId, purchase.GetProperty("shelfId").GetString());
        Assert.Equal(1, purchase.GetProperty("productTypeCode").GetInt32());
        Assert.Equal(4.5, purchase.GetProperty("price").GetDouble());
        Assert.Equal(4, purchase.GetProperty("remainingQuantity").GetInt32());
        Assert.Equal(4, await GetQuantityAsync(machineId));
    }

    [Fact]
    public async Task Purchase_with_an_admin_token_returns_200()
    {
        var (machineId, shelfId) = await CreateMachineWithShelfAsync(quantity: 5);

        var response = await PurchaseAsync(machineId, shelfId, userId: "admin-42", role: "admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Buying_the_last_item_returns_remaining_quantity_0()
    {
        var (machineId, shelfId) = await CreateMachineWithShelfAsync(quantity: 1);

        var response = await PurchaseAsync(machineId, shelfId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("remainingQuantity").GetInt32());
    }

    [Fact]
    public async Task Sold_out_shelf_returns_409_and_stays_at_0()
    {
        var (machineId, shelfId) = await CreateMachineWithShelfAsync(quantity: 1);
        await PurchaseAsync(machineId, shelfId);

        var response = await PurchaseAsync(machineId, shelfId);

        var error = await ErrorAssert.HasErrorAsync(response, HttpStatusCode.Conflict, "Conflict");
        Assert.Equal($"Shelf {shelfId} is empty.", error.GetProperty("message").GetString());
        Assert.Equal(0, await GetQuantityAsync(machineId));
    }

    [Fact]
    public async Task Shelf_that_was_never_loaded_returns_409()
    {
        var (machineId, shelfId) = await CreateMachineWithShelfAsync(quantity: 0);

        var response = await PurchaseAsync(machineId, shelfId);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.Conflict, "Conflict");
    }

    [Fact]
    public async Task Unknown_machine_returns_404()
    {
        var response = await PurchaseAsync("missing", "any-shelf");

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.NotFound, "NotFound");
    }

    [Fact]
    public async Task Unknown_shelf_returns_404()
    {
        var (machineId, _) = await CreateMachineWithShelfAsync(quantity: 5);

        var response = await PurchaseAsync(machineId, "missing");

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.NotFound, "NotFound");
    }

    [Fact]
    public async Task Shelf_of_another_machine_returns_404_and_changes_nothing()
    {
        var (machineId, _) = await CreateMachineWithShelfAsync(quantity: 5);
        var (otherMachineId, otherShelfId) = await CreateMachineWithShelfAsync(quantity: 5);

        var response = await PurchaseAsync(machineId, otherShelfId);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.NotFound, "NotFound");
        Assert.Equal(5, await GetQuantityAsync(otherMachineId));
    }

    [Fact]
    public async Task Purchase_does_not_change_the_audit_fields()
    {
        var (machineId, shelfId) = await CreateMachineWithShelfAsync(quantity: 5);
        var before = await GetMachineAsync(machineId);

        await PurchaseAsync(machineId, shelfId, userId: "user-9");

        var after = await GetMachineAsync(machineId);
        Assert.Equal(before.GetProperty("updatedAt").GetDateTime(), after.GetProperty("updatedAt").GetDateTime());
        Assert.Equal("admin-42", after.GetProperty("updatedBy").GetString());
    }

    [Fact]
    public async Task A_body_sent_anyway_is_ignored()
    {
        var (machineId, shelfId) = await CreateMachineWithShelfAsync(quantity: 5);

        var response = await SendAsync(HttpMethod.Post, $"/machines/{machineId}/shelves/{shelfId}/purchase", """{ "quantity": 3 }""", "user-7", "user");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(4, await GetQuantityAsync(machineId));
    }

    [Fact]
    public async Task Concurrent_purchases_of_the_last_item_give_one_200_and_four_409()
    {
        var (machineId, shelfId) = await CreateMachineWithShelfAsync(quantity: 1);

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => PurchaseAsync(machineId, shelfId)));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(4, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(0, await GetQuantityAsync(machineId));
    }

    [Fact]
    public async Task User_can_get_the_machine_to_see_the_stock()
    {
        var (machineId, shelfId) = await CreateMachineWithShelfAsync(quantity: 5);
        await PurchaseAsync(machineId, shelfId);

        var response = await SendAsync(HttpMethod.Get, $"/machines/{machineId}", null, "user-7", "user");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var shelf = Assert.Single((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("shelves").EnumerateArray());
        Assert.Equal(4, shelf.GetProperty("quantity").GetInt32());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "user")]
    [InlineData("", "user")]
    public async Task Missing_token_or_user_id_returns_401(string? userId, string? role)
    {
        var response = await SendAsync(HttpMethod.Post, "/machines/any-id/shelves/any-shelf/purchase", null, userId, role);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.Unauthorized, "Unauthorized");
    }

    [Fact]
    public async Task Invalid_token_returns_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/machines/any-id/shelves/any-shelf/purchase");
        request.Headers.Authorization = TestTokens.Bearer("not-a-jwt");

        var response = await _client.SendAsync(request);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.Unauthorized, "Unauthorized");
    }

    [Theory]
    [InlineData("User")]
    [InlineData("guest")]
    [InlineData(null)]
    public async Task Unknown_role_returns_403(string? role)
    {
        var response = await SendAsync(HttpMethod.Post, "/machines/any-id/shelves/any-shelf/purchase", null, "user-7", role);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.Forbidden, "Forbidden");
    }

    private async Task<(string MachineId, string ShelfId)> CreateMachineWithShelfAsync(int quantity)
    {
        const string MACHINE = """{ "name": "Riverside Snacks", "location": { "region": "Riverside Market", "zipCode": "10001" } }""";
        var machine = await SendAsync(HttpMethod.Post, "/machines", MACHINE, "admin-42", "admin");
        Assert.Equal(HttpStatusCode.Created, machine.StatusCode);
        var machineId = (await machine.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

        const string SHELF = """{ "productTypeCode": 1, "capacity": 10, "price": 4.5 }""";
        var shelf = await SendAsync(HttpMethod.Post, $"/machines/{machineId}/shelves", SHELF, "admin-42", "admin");
        Assert.Equal(HttpStatusCode.Created, shelf.StatusCode);
        var shelfId = (await shelf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

        if (quantity > 0)
        {
            var load = $$"""{ "shelfId": "{{shelfId}}", "productTypeCode": 1, "quantity": {{quantity}} }""";
            var loaded = await SendAsync(HttpMethod.Post, $"/machines/{machineId}/inventory", load, "admin-42", "admin");
            Assert.Equal(HttpStatusCode.OK, loaded.StatusCode);
        }

        return (machineId, shelfId);
    }

    private async Task<JsonElement> GetMachineAsync(string machineId)
    {
        var response = await SendAsync(HttpMethod.Get, $"/machines/{machineId}", null, "admin-42", "admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<int> GetQuantityAsync(string machineId)
    {
        var shelf = Assert.Single((await GetMachineAsync(machineId)).GetProperty("shelves").EnumerateArray());
        return shelf.GetProperty("quantity").GetInt32();
    }

    private Task<HttpResponseMessage> PurchaseAsync(string machineId, string shelfId, string userId = "user-7", string role = "user") =>
        SendAsync(HttpMethod.Post, $"/machines/{machineId}/shelves/{shelfId}/purchase", null, userId, role);

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? body, string? userId, string? role)
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
}
