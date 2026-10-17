using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Requests.Application.Exceptions;

namespace Requests.Api.Errors;

/// <summary>
/// Turns exceptions into <see cref="ErrorResponse"/> bodies: validation 400, not found 404,
/// malformed request 400, anything else 500. A 500 never shows the exception; it goes to the log only.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case ValidationException validation:
                var response = ErrorResponses.Create(HttpStatusCode.BadRequest, validation.Message);
                response.Error.Details.Add(new ErrorDetail { Field = validation.Field, Issue = validation.Issue });
                await ErrorResponses.WriteAsync(httpContext, HttpStatusCode.BadRequest, response);
                break;

            case NotFoundException notFound:
                await ErrorResponses.WriteAsync(
                    httpContext,
                    HttpStatusCode.NotFound,
                    ErrorResponses.Create(HttpStatusCode.NotFound, notFound.Message));
                break;

            case BadHttpRequestException:
                logger.LogInformation(exception, "Malformed request to {Path}.", httpContext.Request.Path);
                await ErrorResponses.WriteAsync(httpContext, HttpStatusCode.BadRequest, ErrorResponses.Create(HttpStatusCode.BadRequest));
                break;

            default:
                logger.LogError(exception, "Unhandled exception for {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);
                await ErrorResponses.WriteAsync(
                    httpContext,
                    HttpStatusCode.InternalServerError,
                    ErrorResponses.Create(HttpStatusCode.InternalServerError));
                break;
        }

        return true;
    }
}
