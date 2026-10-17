using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Requests.Api.Errors;

namespace Requests.Tests.Api;

/// <summary>No endpoint throws an unexpected exception, so the 500 path is tested on the handler directly.</summary>
public class GlobalExceptionHandlerTests
{
    private const string SECRET_MESSAGE = "Secret: this text must never reach a client.";

    [Fact]
    public async Task Unexpected_exception_returns_500_without_details()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(httpContext, new InvalidOperationException(SECRET_MESSAGE), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        httpContext.Response.Body.Position = 0;
        var body = await JsonDocument.ParseAsync(httpContext.Response.Body);
        var error = body.RootElement.GetProperty("error");
        Assert.Equal("InternalServerError", error.GetProperty("code").GetString());
        Assert.Equal("Internal Server Error", error.GetProperty("message").GetString());
        Assert.DoesNotContain(SECRET_MESSAGE, body.RootElement.GetRawText());
        Assert.DoesNotContain(nameof(InvalidOperationException), body.RootElement.GetRawText());
    }
}
