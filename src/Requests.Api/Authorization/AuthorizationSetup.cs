using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Requests.Infrastructure.Auth;

namespace Requests.Api.Authorization;

/// <summary>
/// Bearer-token authentication for tokens from POST /auth/login. A missing or invalid token (including one
/// without "sub") gets 401, a valid token without the needed role gets 403. Both responses have no body,
/// so status code pages fill in the standard error body.
/// </summary>
public static class AuthorizationSetup
{
    public static IServiceCollection AddJwtAuthorization(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Settings are read when the options are built, not here, so test hosts can override them.
        // ValidateOnStart builds them at startup, so a missing or short signing key stops the app.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtSettings>((o, settings) =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKey = settings.CreateSigningKey(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = JwtSettings.ROLE_CLAIM,
                };

                // A token that does not say who the user is does not authenticate anyone.
                o.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (string.IsNullOrWhiteSpace(context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value))
                        {
                            context.Fail("The token has no sub claim.");
                        }

                        return Task.CompletedTask;
                    },
                };
            })
            .ValidateOnStart();

        services.AddAuthorization();

        return services;
    }
}
