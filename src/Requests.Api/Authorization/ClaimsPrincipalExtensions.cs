using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Requests.Api.Authorization;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The token's "sub" claim. Authentication rejects tokens without it, so any authenticated user has one.</summary>
    public static string GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("The user has no sub claim.");
    }
}
