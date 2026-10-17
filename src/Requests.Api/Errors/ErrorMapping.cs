using System.Diagnostics;

namespace Requests.Api.Errors;

/// <summary>
/// Final pass over every ProblemDetails response. Guarantees <c>code</c> and <c>detail</c> are present,
/// so framework errors (unknown route, wrong method, malformed request, crash) look like business errors.
/// </summary>
public static class ErrorMapping
{
    public const string CodeKey = "code";

    public const string TraceIdKey = "traceId";

    public const string UnexpectedDetail = "An unexpected error occurred.";

    public static void AddCodeAndDefaults(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        var status = problem.Status ?? context.HttpContext.Response.StatusCode;
        var (code, detail) = DefaultsFor(status, context.HttpContext.Request.Method);

        // Server errors never leak details, whatever set them.
        if (status >= StatusCodes.Status500InternalServerError)
        {
            problem.Detail = UnexpectedDetail;
        }

        problem.Detail ??= detail;
        problem.Extensions.TryAdd(CodeKey, code);

        // Links the response to the log entry for the same request.
        problem.Extensions.TryAdd(TraceIdKey, Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
    }

    private static (string Code, string Detail) DefaultsFor(int status, string method) => status switch
    {
        StatusCodes.Status400BadRequest => ("request.malformed", "The request body or parameters could not be read."),
        StatusCodes.Status404NotFound => ("route.not_found", "No resource exists at this path."),
        StatusCodes.Status405MethodNotAllowed => ("route.method_not_allowed", $"This path does not support the {method} method."),
        >= StatusCodes.Status500InternalServerError => ("server.unexpected", UnexpectedDetail),
        _ => ($"http.status_{status}", "The request could not be processed."),
    };
}
