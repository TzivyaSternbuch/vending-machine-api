using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Requests.Api.Authorization;

/// TODO
/// <summary>
/// Temporary header-based check until real authentication: X-User-Id and X-User-Role must be present
/// (401 otherwise) and the role must be admin (403 otherwise). It is an authorization filter, so it
/// runs before the request body is read and validated.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminOnlyAttribute : Attribute, IAuthorizationFilter
{
    public const string USER_ID_HEADER = "X-User-Id";

    public const string USER_ROLE_HEADER = "X-User-Role";

    public const string ADMIN_ROLE = "admin";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var headers = context.HttpContext.Request.Headers;
        string? userId = headers[USER_ID_HEADER];
        string? role = headers[USER_ROLE_HEADER];

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status401Unauthorized);
            return;
        }

        if (role != ADMIN_ROLE)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        }
    }
}
