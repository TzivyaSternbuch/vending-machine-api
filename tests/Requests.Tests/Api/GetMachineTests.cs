using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Requests.Tests.Api;

/// <summary>Integration tests for GET /machines/{id}. A new API (and so a new, empty repository) for every test.</summary>
public class GetMachineTests : IDisposable
{
    private const string VALID_BODY = """
        {
          "name": "Riverside Snacks",
          "location": { "region": "Riverside Market", "zipCode": "01234", "description": "by the well" },
          "maxShelves": 6
        }
        """;

    private readonly ApiFactory _factory = new();

    private readonly HttpClient _client;

    public GetMachineTests()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = TestTokens.Admin("admin-42");
    }

    [Fact]
    public async Task Get_returns_the_created_machine()
    {
        var createResponse = await _client.PostAsync("/machines", new StringContent(VALID_BODY, Encoding.UTF8, "application/json"));
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var response = await _client.GetAsync($"/machines/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var found = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(created.GetRawText(), found.GetRawText());
    }

    [Fact]
    public async Task Get_with_unknown_id_returns_404()
    {
        var response = await _client.GetAsync("/machines/missing");

        var error = await ErrorAssert.HasErrorAsync(response, HttpStatusCode.NotFound, "NotFound");
        Assert.Equal("Machine 'missing' was not found.", error.GetProperty("message").GetString());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
