# Requests API

.NET 8 Web API. T0 is the skeleton: solution layout and one error format for every failure.

## Commands

| Action      | Command                                   |
| ----------- | ----------------------------------------- |
| Build       | `dotnet build`                            |
| Run the API | `dotnet run --project src/Requests.Api`   |
| Run tests   | `dotnet test`                             |

Warnings fail the build (`TreatWarningsAsErrors` in `Directory.Build.props`).

## Layout

- `src/Requests.Domain` – entities and business rules. References nothing.
- `src/Requests.Application` – use cases and the interfaces they need.
- `src/Requests.Infrastructure` – implementations of Application interfaces.
- `src/Requests.Api` – HTTP only: endpoints, error mapping, exception handling.
- `tests/Requests.Tests` – unit tests and in-memory API tests (xUnit + `WebApplicationFactory`).

## Errors

Services throw `ValidationException(field, issue)` or `NotFoundException(message)` (`Requests.Application/Exceptions`).
`Requests.Api/Errors/GlobalExceptionHandler.cs` maps them to status codes; status code pages cover responses with no body.

Every error response is `application/json` in one shape. `code` is the `HttpStatusCode` name; `message` is the
exception's message for validation and not found, otherwise the standard reason phrase (e.g. "Not Found"):

```json
{ "error": { "code": "BadRequest", "message": "name is required", "details": [ { "field": "name", "issue": "is required" } ] } }
```

| Source                                  | Status | Code                  |
| --------------------------------------- | ------ | --------------------- |
| `ValidationException`                   | 400    | `BadRequest`          |
| Malformed JSON / bad binding            | 400    | `BadRequest`          |
| Missing `X-User-Id` / `X-User-Role`     | 401    | `Unauthorized`        |
| Role is not `admin`                     | 403    | `Forbidden`           |
| `NotFoundException` / unknown route     | 404    | `NotFound`            |
| Wrong HTTP method                       | 405    | `MethodNotAllowed`    |
| Unhandled exception                     | 500    | `InternalServerError` |

`details` is always an array; only `ValidationException` fills it. A 500 never shows the
exception; it goes to the log only.
