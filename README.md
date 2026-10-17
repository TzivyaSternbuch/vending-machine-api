# Requests API

.NET 8 Web API. T0 is the skeleton: solution layout and one error format for every failure.

## Commands

| Action      | Command                                   |
| ----------- | ----------------------------------------- |
| Build       | `dotnet build`                            |
| Run the API | `dotnet run --project src/Requests.Api`   |
| Run tests   | `dotnet test`                             |
| Add a migration | `dotnet tool restore` then `dotnet ef migrations add <Name> --project src/Requests.Infrastructure --output-dir Persistence/Migrations` |

Warnings fail the build (`TreatWarningsAsErrors` in `Directory.Build.props`).

## Database

SQLite through EF Core. The file is `ConnectionStrings:Default` in `appsettings.json` (`Data Source=vending.db`);
pending migrations run at startup. Categories and product types are seeded by a migration (`ProductTypeSeed.cs`).
Every test gets its own temporary SQLite file, migrated on creation and deleted after.

## Layout

- `src/Requests.Domain` – entities and business rules. References nothing.
- `src/Requests.Application` – use cases and the interfaces they need.
- `src/Requests.Infrastructure` – implementations of Application interfaces.
- `src/Requests.Api` – HTTP only: endpoints, error mapping, exception handling.
- `tests/Requests.Tests` – unit tests and in-process API tests (xUnit + `WebApplicationFactory`).

## Trying it in Swagger

1. `dotnet run --project src/Requests.Api` and open http://localhost:5080/swagger.
2. `POST /auth/login` with `{ "username": "admin" }` (or `"user"`) and copy `accessToken` from the response.
3. Click **Authorize**, paste the token, and click **Authorize** again.
4. Call `POST /machines` and `GET /machines/{id}`. Admin gets 201 / 200, user gets 403, no token gets 401.

| Username | Role    |
| -------- | ------- |
| `admin`  | `admin` |
| `user`   | `user`  |

The accounts are seeded by a migration (`Requests.Infrastructure/Users/UserSeed.cs`).

## Authentication

`POST /auth/login` (`{ "username" }`) returns `{ "accessToken", "expiresAt" }`; a token lasts one hour. This is
a test login: there are no passwords, so anyone who knows a username gets its token. An unknown username gets 401.

Every `/machines`, `/product-categories` and `/product-types` endpoint needs `Authorization: Bearer <accessToken>`. A token must be HS256-signed with
`JWT_SIGNING_KEY`, have issuer `JWT_ISSUER` and audience `JWT_AUDIENCE`, not be expired, and carry:

| Claim  | Meaning                                              |
| ------ | ---------------------------------------------------- |
| `sub`  | user id, recorded as `createdBy` / `updatedBy`       |
| `role` | must be `admin`                                      |

All three settings are in `appsettings.json`, so the API runs as-is. The signing key there is a public test
key; a real deployment overrides it with the `JWT_SIGNING_KEY` environment variable (at least 32 characters).

## Errors

Services throw `ValidationException(field, issue)`, `NotFoundException(message)` or `ConflictException(message)` (`Requests.Application/Exceptions`).
`Requests.Api/Errors/GlobalExceptionHandler.cs` maps them to status codes; status code pages cover responses with no body.

Every error response is `application/json` in one shape. `code` is the `HttpStatusCode` name; `message` is the
exception's message for validation and not found, otherwise the standard reason phrase (e.g. "Not Found"):

```json
{ "error": { "code": "BadRequest", "message": "name is required", "details": [ { "field": "name", "issue": "is required" } ] } }
```

| Source                                    | Status | Code                  |
| ----------------------------------------- | ------ | --------------------- |
| `ValidationException`                     | 400    | `BadRequest`          |
| Malformed JSON / bad binding              | 400    | `BadRequest`          |
| Missing / invalid bearer token, no `sub`  | 401    | `Unauthorized`        |
| `UnauthorizedException` (wrong login)     | 401    | `Unauthorized`        |
| Role is not `admin`                       | 403    | `Forbidden`           |
| `NotFoundException` / unknown route       | 404    | `NotFound`            |
| Wrong HTTP method                         | 405    | `MethodNotAllowed`    |
| `ConflictException` (e.g. name taken)     | 409    | `Conflict`            |
| Unhandled exception                       | 500    | `InternalServerError` |

`details` is always an array; only `ValidationException` fills it. A 500 never shows the
exception; it goes to the log only.
