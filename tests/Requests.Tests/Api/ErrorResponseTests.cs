using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Requests.Api.Diagnostics;

namespace Requests.Tests.Api;

public class ErrorResponseTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Unknown_route_returns_problem()
    {
        var response = await _client.GetAsync("/does-not-exist");

        var problem = await AssertProblemAsync(response, HttpStatusCode.NotFound, "route.not_found");
        Assert.True(problem.TryGetProperty("title", out _));
    }

    [Theory]
    [InlineData("validation", HttpStatusCode.BadRequest, "diagnostics.invalid")]
    [InlineData("not-found", HttpStatusCode.NotFound, "diagnostics.not_found")]
    [InlineData("conflict", HttpStatusCode.Conflict, "diagnostics.conflict")]
    public async Task Each_category_maps_to_status(string category, HttpStatusCode expectedStatus, string expectedCode)
    {
        var response = await _client.GetAsync($"/_diagnostics/errors/{category}");

        await AssertProblemAsync(response, expectedStatus, expectedCode);
    }

    [Fact]
    public async Task Unhandled_exception_hides_details()
    {
        var response = await _client.GetAsync("/_diagnostics/errors/unexpected");

        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError, "server.unexpected");
        Assert.Equal("An unexpected error occurred.", problem.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
        Assert.DoesNotContain(DiagnosticsEndpoints.ExceptionMessage, problem.GetRawText());
        Assert.DoesNotContain(nameof(InvalidOperationException), problem.GetRawText());
    }

    [Fact]
    public async Task Malformed_body_returns_400()
    {
        using var content = new StringContent("{ not json", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/_diagnostics/echo", content);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "request.malformed");
    }

    [Fact]
    public async Task Wrong_method_returns_405()
    {
        var response = await _client.DeleteAsync("/_diagnostics/errors/validation");

        await AssertProblemAsync(response, HttpStatusCode.MethodNotAllowed, "route.method_not_allowed");
    }

    [Fact]
    public async Task Success_is_not_a_problem()
    {
        var response = await _client.GetAsync("/_diagnostics/errors/ok");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", await response.Content.ReadFromJsonAsync<string>());
    }

    private static async Task<JsonElement> AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
        return problem;
    }
}
