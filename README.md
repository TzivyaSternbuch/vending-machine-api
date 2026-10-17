# Requests API

.NET 8 Web API. T0 is the skeleton: solution layout, domain error model and one error format for every failure.

## Commands

| Action      | Command                                   |
| ----------- | ----------------------------------------- |
| Build       | `dotnet build`                            |
| Run the API | `dotnet run --project src/Requests.Api`   |
| Run tests   | `dotnet test`                             |

Warnings fail the build (`TreatWarningsAsErrors` in `Directory.Build.props`).

## Layout

- `src/Requests.Domain` – entities, business rules, `Error` / `ErrorType` / `Result`. References nothing.
- `src/Requests.Application` – use cases and the interfaces they need.
- `src/Requests.Infrastructure` – implementations of Application interfaces.
- `src/Requests.Api` – HTTP only: endpoints, error mapping, exception handling.
- `tests/Requests.Tests` – unit tests and in-memory API tests (xUnit + `WebApplicationFactory`).

## Errors

Expected failures return `Result.Failure(Error.X("<area>.<reason>", message))`; they never throw.
`Requests.Api/Errors/ResultExtensions.cs` is the only place that maps an `ErrorType` to a status code.

Every error response is RFC 7807 ProblemDetails (`application/problem+json`) with an extra `code`:

| Source                          | Status | Code                       |
| ------------------------------- | ------ | -------------------------- |
| `ErrorType.Validation`          | 400    | from the `Error`           |
| `ErrorType.NotFound`            | 404    | from the `Error`           |
| `ErrorType.Conflict`            | 409    | from the `Error`           |
| Unknown route                   | 404    | `route.not_found`          |
| Wrong HTTP method               | 405    | `route.method_not_allowed` |
| Malformed JSON / bad binding    | 400    | `request.malformed`        |
| Unhandled exception             | 500    | `server.unexpected`        |

500 responses always say "An unexpected error occurred."; the exception goes to the log, linked by `traceId`.
