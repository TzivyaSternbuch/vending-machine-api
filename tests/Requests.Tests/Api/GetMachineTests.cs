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
        _client.DefaultRequestHeaders.Add("X-User-Id", "admin-42");
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
    public async Task Get_with_unknown_id_fails()
    {
        var response = await _client.GetAsync("/machines/missing");

        //todo: The service throws NotFoundException. It is not mapped to 404 yet (separate error-handling task),
        // The service throws NotFoundException. It is not mapped to 404 yet (separate error-handling task),
        // so it reaches the global exception handler as 500. Change to NotFound when the mapping lands.
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
