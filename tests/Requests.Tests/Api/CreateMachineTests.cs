using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Requests.Tests.Api;

/// <summary>A new API (and so a new, empty repository) for every test.</summary>
public class CreateMachineTests : IDisposable
{
    private const string VALID_BODY = """
        {
          "name": "Riverside Snacks",
          "location": { "region": "Riverside Market", "zipCode": "10001", "description": "by the well" },
          "maxShelves": 6
        }
        """;

    private readonly ApiFactory _factory = new();

    private readonly HttpClient _client;

    public CreateMachineTests()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-User-Id", "admin-42");
        _client.DefaultRequestHeaders.Add("X-User-Role", "admin");
    }

    [Fact]
    public async Task Create_returns_201_with_machine()
    {
        var response = await PostAsync(VALID_BODY);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var machine = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(machine.GetProperty("id").GetString()));
        Assert.Equal("Riverside Snacks", machine.GetProperty("name").GetString());
        Assert.Equal("Riverside Market", machine.GetProperty("location").GetProperty("region").GetString());
        Assert.Equal("10001", machine.GetProperty("location").GetProperty("zipCode").GetString());
        Assert.Equal("by the well", machine.GetProperty("location").GetProperty("description").GetString());
        Assert.Equal(6, machine.GetProperty("maxShelves").GetInt32());
        Assert.Equal(0, machine.GetProperty("shelves").GetArrayLength());
        Assert.EndsWith("Z", machine.GetProperty("createdAt").GetString());
        Assert.Equal(machine.GetProperty("createdAt").GetString(), machine.GetProperty("updatedAt").GetString());
        Assert.Equal("admin-42", machine.GetProperty("createdBy").GetString());
        Assert.Equal("admin-42", machine.GetProperty("updatedBy").GetString());
    }

    [Fact]
    public async Task Missing_maxShelves_returns_20()
    {
        var response = await PostAsync("""
            { "name": "Riverside Snacks", "location": { "region": "Riverside Market", "zipCode": "10001" } }
            """);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var machine = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(20, machine.GetProperty("maxShelves").GetInt32());
        Assert.Equal(JsonValueKind.Null, machine.GetProperty("location").GetProperty("description").ValueKind);
    }

    [Fact]
    public async Task Client_supplied_server_fields_are_ignored()
    {
        var response = await PostAsync("""
            {
              "id": "m-1",
              "name": "Riverside Snacks",
              "location": { "region": "Riverside Market", "zipCode": "10001" },
              "shelves": [{}],
              "createdBy": "someone",
              "updatedBy": "someone"
            }
            """);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var machine = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual("m-1", machine.GetProperty("id").GetString());
        Assert.Equal(0, machine.GetProperty("shelves").GetArrayLength());
        Assert.Equal("admin-42", machine.GetProperty("createdBy").GetString());
        Assert.Equal("admin-42", machine.GetProperty("updatedBy").GetString());
    }

    [Theory]
    [InlineData("null")]
    [InlineData("2.5")]
    public async Task MaxShelves_that_is_not_an_integer_returns_400(string maxShelves)
    {
        var response = await PostAsync($$"""
            { "name": "Riverside Snacks", "location": { "region": "Riverside Market", "zipCode": "10001" }, "maxShelves": {{maxShelves}} }
            """);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.BadRequest, "BadRequest");
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("")]
    public async Task Malformed_body_returns_400(string body)
    {
        var response = await PostAsync(body);

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.BadRequest, "BadRequest");
    }

    [Theory]
    [InlineData("""{ "name": "", "location": { "region": "Riverside Market", "zipCode": "10001" } }""", "name", "is required")]
    [InlineData("""{ "name": "   ", "location": { "region": "Riverside Market", "zipCode": "10001" } }""", "name", "is required")]
    [InlineData("""{ "name": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "location": { "region": "Riverside Market", "zipCode": "10001" } }""", "name", "must be at most 64 characters")]
    [InlineData("""{ "name": "Riverside Snacks" }""", "location", "is required")]
    [InlineData("""{ "name": "Riverside Snacks", "location": { "region": "Riverside Market" } }""", "location.zipCode", "is required")]
    [InlineData("""{ "name": "Riverside Snacks", "location": { "region": "Riverside Market", "zipCode": "1234" } }""", "location.zipCode", "must be 5 to 10 digits")]
    [InlineData("""{ "name": "Riverside Snacks", "location": { "region": "Riverside Market", "zipCode": "12345678901" } }""", "location.zipCode", "must be 5 to 10 digits")]
    [InlineData("""{ "name": "Riverside Snacks", "location": { "region": "Riverside Market", "zipCode": "1000A" } }""", "location.zipCode", "must be 5 to 10 digits")]
    [InlineData("""{ "name": "Riverside Snacks", "location": { "region": "Riverside Market", "zipCode": "10001" }, "maxShelves": 0 }""", "maxShelves", "must be between 1 and 20")]
    [InlineData("""{ "name": "Riverside Snacks", "location": { "region": "Riverside Market", "zipCode": "10001" }, "maxShelves": 21 }""", "maxShelves", "must be between 1 and 20")]
    public async Task Invalid_machine_returns_400_with_field_and_issue(string body, string expectedField, string expectedIssue)
    {
        var response = await PostAsync(body);

        var error = await ErrorAssert.HasErrorAsync(response, HttpStatusCode.BadRequest, "BadRequest");
        Assert.Equal($"{expectedField} {expectedIssue}", error.GetProperty("message").GetString());
        var detail = Assert.Single(error.GetProperty("details").EnumerateArray());
        Assert.Equal(expectedField, detail.GetProperty("field").GetString());
        Assert.Equal(expectedIssue, detail.GetProperty("issue").GetString());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private Task<HttpResponseMessage> PostAsync(string body) =>
        _client.PostAsync("/machines", new StringContent(body, Encoding.UTF8, "application/json"));
}
