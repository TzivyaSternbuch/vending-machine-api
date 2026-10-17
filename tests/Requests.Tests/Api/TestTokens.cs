using System.Net.Http.Headers;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Requests.Domain.Users;
using Requests.Infrastructure.Auth;

namespace Requests.Tests.Api;

/// <summary>Signs bearer tokens directly (bypassing login), with the settings <see cref="ApiFactory"/> gives the API.</summary>
public static class TestTokens
{
    public const string ISSUER = "test-issuer";

    public const string AUDIENCE = "test-audience";

    public const string SIGNING_KEY = "test-signing-key-that-is-at-least-32-chars";

    public const string OTHER_SIGNING_KEY = "another-signing-key-that-is-at-least-32-chars";

    public static string Create(
        string? userId,
        string? role,
        string issuer = ISSUER,
        string audience = AUDIENCE,
        string signingKey = SIGNING_KEY,
        TimeSpan? expiresIn = null)
    {
        var claims = new Dictionary<string, object>();

        if (userId is not null)
        {
            claims[JwtRegisteredClaimNames.Sub] = userId;
        }

        if (role is not null)
        {
            claims[JwtSettings.ROLE_CLAIM] = role;
        }

        // Every token is valid for two hours ending at its expiry, so a negative expiresIn gives an expired token.
        var expires = DateTime.UtcNow + (expiresIn ?? TimeSpan.FromHours(1));
        var issuedAt = expires - TimeSpan.FromHours(2);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = claims,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    public static AuthenticationHeaderValue Admin(string userId = "admin-42") => Bearer(Create(userId, Roles.ADMIN));
}
