using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Requests.Application.Auth;
using Requests.Domain.Users;

namespace Requests.Infrastructure.Auth;

/// <summary>HS256 tokens with "sub" = user id and "role" = user role, valid for <see cref="JwtSettings.TOKEN_LIFETIME"/>.</summary>
public class JwtTokenIssuer(JwtSettings settings, TimeProvider timeProvider) : ITokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    public LoginResponse Issue(User user)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now + JwtSettings.TOKEN_LIFETIME;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id,
                [JwtSettings.ROLE_CLAIM] = user.Role,
            },
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(settings.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        };

        return new LoginResponse
        {
            AccessToken = _handler.CreateToken(descriptor),
            ExpiresAt = expiresAt,
        };
    }
}
