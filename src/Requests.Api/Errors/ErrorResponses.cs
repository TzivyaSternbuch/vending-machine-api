using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Requests.Api.Errors;

/// <summary>
/// Builds and writes <see cref="ErrorResponse"/> bodies. The code is the <see cref="HttpStatusCode"/> name
/// (e.g. "NotFound"); without a specific message, the message is the standard reason phrase ("Not Found").
/// </summary>
public static class ErrorResponses
{
    public static ErrorResponse Create(HttpStatusCode status, string message)
    {
        var response = new ErrorResponse();
        response.Error.Code = status.ToString();
        response.Error.Message = message;
        return response;
    }

    public static ErrorResponse Create(HttpStatusCode status)
    {
        return Create(status, ReasonPhrases.GetReasonPhrase((int)status));
    }

    public static async Task WriteAsync(HttpContext httpContext, HttpStatusCode status, ErrorResponse response)
    {
        httpContext.Response.StatusCode = (int)status;
        await httpContext.Response.WriteAsJsonAsync(response);
    }

    /// <summary>MVC calls this when the request body cannot be bound to the action's parameter.</summary>
    public static IActionResult InvalidModelState(ActionContext context)
    {
        return new BadRequestObjectResult(Create(HttpStatusCode.BadRequest));
    }

    /// <summary>Fills in responses that have a status but no body (unknown route, wrong method, 401, 403).</summary>
    public static async Task WriteStatusCodePageAsync(StatusCodeContext context)
    {
        var status = (HttpStatusCode)context.HttpContext.Response.StatusCode;

        await WriteAsync(context.HttpContext, status, Create(status));
    }
}
