using System.Net;

namespace Requests.Tests.Api;

/// <summary>
/// Errors produced by routing rather than by an endpoint. Validation, not found, malformed body and
/// 401/403 are covered by the machine endpoint tests.
/// </summary>
public class ErrorResponseTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Unknown_route_returns_404()
    {
        var response = await _client.GetAsync("/does-not-exist");

        await ErrorAssert.HasErrorAsync(response, HttpStatusCode.NotFound, "NotFound");
    }

    [Fact]
    public async Task Wrong_method_returns_405()
    {
        var response = await _client.DeleteAsync("/machines");

        var error = await ErrorAssert.HasErrorAsync(response, HttpStatusCode.MethodNotAllowed, "MethodNotAllowed");
        Assert.Equal("Method Not Allowed", error.GetProperty("message").GetString());
    }
}
