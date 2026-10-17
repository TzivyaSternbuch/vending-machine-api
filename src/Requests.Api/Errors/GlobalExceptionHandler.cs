using Microsoft.AspNetCore.Diagnostics;

namespace Requests.Api.Errors;

/// <summary>
/// Turns exceptions into ProblemDetails. Malformed requests become 400; anything else becomes 500.
/// The exception itself goes to the log only; <see cref="ErrorMapping"/> fills in code and detail.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int status;
        if (exception is BadHttpRequestException)
        {
            logger.LogInformation(exception, "Malformed request to {Path}.", httpContext.Request.Path);
            status = StatusCodes.Status400BadRequest;
        }
        else
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);
            status = StatusCodes.Status500InternalServerError;
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = { Status = status },
        });
    }
}
