using Requests.Domain.Errors;

namespace Requests.Api.Errors;

/// <summary>The only place that turns an <see cref="ErrorType"/> into an HTTP status code.</summary>
public static class ResultExtensions
{
    public static IResult ToProblem(this Error error) =>
        Results.Problem(
            statusCode: error.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError,
            },
            detail: error.Message,
            extensions: new Dictionary<string, object?> { [ErrorMapping.CodeKey] = error.Code });
}
