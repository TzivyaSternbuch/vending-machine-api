namespace Requests.Api.Errors;

/// <summary>The body of every error response: <c>{ "error": { ... } }</c>.</summary>
public class ErrorResponse
{
    public ErrorBody Error { get; set; } = new();
}
