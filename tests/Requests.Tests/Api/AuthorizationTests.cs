using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Requests.Tests.Api;

/// <summary>Integration tests for the bearer token and admin policy on both machine endpoints.</summary>
public class AuthorizationTests : IDisposable
{
    private const string VALID_BODY = """
        { "name": "Riverside Snacks", "location": { "region": "Riverside Market", "zipCode": "10001" } }
        """;

    private readonly ApiFactory _factory = new();

    private readonly HttpClient _client;

    public AuthorizationTests()
    {
        _client = _factory.CreateClient();
    }

    public static TheoryData<string> InvalidTokens => new()
    {
        "not-a-jwt",
        TestTokens.Create("admin-42", "admin", signingKey: TestTokens.OTHER_SIGNING_KEY),
        TestTokens.Create("admin-42", "admin", issuer: "someone-else"),
        TestTokens.Create("admin-42", "admin", audience: "another-api"),
        TestTokens.Create("admin-42", "admin", expiresIn: TimeSpan.FromHours(-1)),
        TestTokens.Create(null, "admin"),
        TestTokens.Create("", "admin"),
    };

    [Fact]
    public async Task Missing_token_returns_401_on_both_endpoints()
    {
        await AssertBothEndpointsReturnAsync(null, HttpStatusCode.Unauthorized, "Unauthorized");
    }

    [Fact]
    public async Task Non_bearer_scheme_returns_401_on_both_endpoints()
    {
        await AssertBothEndpointsReturnAsync(new AuthenticationHeaderValue("Basic", "YWRtaW46YWRtaW4="), HttpStatusCode.Unauthorized, "Unauthorized");
    }

    [Theory]
    [MemberData(nameof(InvalidTokens))]
    public async Task Invalid_token_returns_401_on_both_endpoints(string token)
    {
        await AssertBothEndpointsReturnAsync(TestTokens.Bearer(token), HttpStatusCode.Unauthorized, "Unauthorized");
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("User")]
    [InlineData(null)]
    public async Task Valid_token_without_a_known_role_returns_403_on_both_endpoints(string? role)
    {
        await AssertBothEndpointsReturnAsync(TestTokens.Bearer(TestTokens.Create("user-7", role)), HttpStatusCode.Forbidden, "Forbidden");
    }

    [Fact]
    public async Task User_role_can_get_a_machine_but_not_create_one()
    {
        var authorization = TestTokens.Bearer(TestTokens.Create("user-7", "user"));

        var createResponse = await SendAsync(HttpMethod.Post, "/machines", VALID_BODY, authorization);
        await ErrorAssert.HasErrorAsync(createResponse, HttpStatusCode.Forbidden, "Forbidden");

        var created = await (await SendAsync(HttpMethod.Post, "/machines", VALID_BODY, TestTokens.Admin()))
            .Content.ReadFromJsonAsync<JsonElement>();
        var getResponse = await SendAsync(HttpMethod.Get, $"/machines/{created.GetProperty("id").GetString()}", null, authorization);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Missing_token_is_checked_before_the_body()
    {
        var response = await SendAsync(HttpMethod.Post, "/machines", "{ not json", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Non_admin_role_is_checked_before_the_body()
    {
        var response = await SendAsync(HttpMethod.Post, "/machines", "{ not json", TestTokens.Bearer(TestTokens.Create("user-7", "user")));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_use_both_endpoints_and_user_id_is_recorded()
    {
        var createResponse = await SendAsync(HttpMethod.Post, "/machines", VALID_BODY, TestTokens.Admin("admin-42"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("admin-42", created.GetProperty("createdBy").GetString());
        Assert.Equal("admin-42", created.GetProperty("updatedBy").GetString());

        var getResponse = await SendAsync(HttpMethod.Get, $"/machines/{created.GetProperty("id").GetString()}", null, TestTokens.Admin("admin-7"));
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task AssertBothEndpointsReturnAsync(AuthenticationHeaderValue? authorization, HttpStatusCode status, string code)
    {
        var createResponse = await SendAsync(HttpMethod.Post, "/machines", VALID_BODY, authorization);
        var getResponse = await SendAsync(HttpMethod.Get, "/machines/any-id", null, authorization);

        await ErrorAssert.HasErrorAsync(createResponse, status, code);
        await ErrorAssert.HasErrorAsync(getResponse, status, code);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? body, AuthenticationHeaderValue? authorization)
    {
        var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        request.Headers.Authorization = authorization;

        return _client.SendAsync(request);
    }
}
