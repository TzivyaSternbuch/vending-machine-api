using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Requests.Tests.Api;

/// <summary>Integration tests for the X-User-Id / X-User-Role check on both machine endpoints.</summary>
public class AdminOnlyTests : IDisposable
{
    private const string VALID_BODY = """
        { "name": "Riverside Snacks", "location": { "region": "Riverside Market", "zipCode": "10001" } }
        """;

    private readonly ApiFactory _factory = new();

    private readonly HttpClient _client;

    public AdminOnlyTests()
    {
        _client = _factory.CreateClient();
    }

    [Theory]
    [InlineData(null, "admin")]
    [InlineData("admin-42", null)]
    [InlineData("", "admin")]
    [InlineData("admin-42", "")]
    [InlineData(null, null)]
    public async Task Missing_or_empty_header_returns_401_on_both_endpoints(string? userId, string? role)
    {
        var createResponse = await SendAsync(HttpMethod.Post, "/machines", VALID_BODY, userId, role);
        var getResponse = await SendAsync(HttpMethod.Get, "/machines/any-id", null, userId, role);

        Assert.Equal(HttpStatusCode.Unauthorized, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("Admin")]
    public async Task Non_admin_role_returns_403_on_both_endpoints(string role)
    {
        var createResponse = await SendAsync(HttpMethod.Post, "/machines", VALID_BODY, "user-7", role);
        var getResponse = await SendAsync(HttpMethod.Get, "/machines/any-id", null, "user-7", role);

        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);
    }

    [Fact]
    public async Task Missing_header_is_checked_before_the_body()
    {
        var response = await SendAsync(HttpMethod.Post, "/machines", "{ not json", null, "admin");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Non_admin_role_is_checked_before_the_body()
    {
        var response = await SendAsync(HttpMethod.Post, "/machines", "{ not json", "user-7", "user");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_use_both_endpoints_and_user_id_is_recorded()
    {
        var createResponse = await SendAsync(HttpMethod.Post, "/machines", VALID_BODY, "admin-42", "admin");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("admin-42", created.GetProperty("createdBy").GetString());
        Assert.Equal("admin-42", created.GetProperty("updatedBy").GetString());

        var getResponse = await SendAsync(HttpMethod.Get, $"/machines/{created.GetProperty("id").GetString()}", null, "admin-7", "admin");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? body, string? userId, string? role)
    {
        var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        if (userId is not null)
        {
            request.Headers.Add("X-User-Id", userId);
        }

        if (role is not null)
        {
            request.Headers.Add("X-User-Role", role);
        }

        return _client.SendAsync(request);
    }
}
