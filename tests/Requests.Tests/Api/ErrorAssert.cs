using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Requests.Tests.Api;

/// <summary>Checks the <c>{ "error": { "code", "message", "details" } }</c> body shared by every error response.</summary>
public static class ErrorAssert
{
    /// <returns>The inner <c>error</c> object.</returns>
    public static async Task<JsonElement> HasErrorAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = body.GetProperty("error");
        Assert.Equal(expectedCode, error.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("message").GetString()));
        Assert.Equal(JsonValueKind.Array, error.GetProperty("details").ValueKind);
        return error;
    }
}
