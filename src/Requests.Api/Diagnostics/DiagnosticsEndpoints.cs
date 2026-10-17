using Requests.Api.Errors;
using Requests.Domain.Errors;

namespace Requests.Api.Diagnostics;

/// <summary>
/// Endpoints that trigger each error category so tests can exercise the error pipeline before real
/// endpoints exist. Mapped only in the Testing environment.
/// </summary>
public static class DiagnosticsEndpoints
{
    public const string EnvironmentName = "Testing";

    public const string ExceptionMessage = "Diagnostics secret: this text must never reach a client.";

    public static IEndpointRouteBuilder MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/_diagnostics");

        group.MapGet("/errors/{category}", (string category) =>
        {
            var result = Trigger(category);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
        });

        group.MapPost("/echo", (EchoBody body) => Results.Ok(body));

        return app;
    }

    private static Result<string> Trigger(string category) => category switch
    {
        "ok" => Result.Success("ok"),
        "validation" => Result.Failure<string>(Error.Validation("diagnostics.invalid", "The diagnostic input is invalid.")),
        "not-found" => Result.Failure<string>(Error.NotFound("diagnostics.not_found", "The diagnostic item was not found.")),
        "conflict" => Result.Failure<string>(Error.Conflict("diagnostics.conflict", "The diagnostic item is in the wrong state.")),
        "unexpected" => throw new InvalidOperationException(ExceptionMessage),
        _ => Result.Failure<string>(Error.NotFound("diagnostics.unknown_category", $"Unknown diagnostic category '{category}'.")),
    };

    public record EchoBody(string Text);
}
