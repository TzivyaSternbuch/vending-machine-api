using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Requests.Infrastructure.Auth;

/// <summary>
/// How tokens are signed (by <see cref="JwtTokenIssuer"/>) and validated (by the API). appsettings.json holds a
/// public test signing key so the API runs as-is; a real deployment overrides it with the environment variable
/// JWT_SIGNING_KEY. Startup fails if any value is missing.
/// </summary>
public class JwtSettings
{
    public const string ISSUER_KEY = "JWT_ISSUER";

    public const string AUDIENCE_KEY = "JWT_AUDIENCE";

    public const string SIGNING_KEY_KEY = "JWT_SIGNING_KEY";

    /// <summary>HS256 needs a key of at least 256 bits.</summary>
    public const int MIN_SIGNING_KEY_LENGTH = 32;

    public const string ROLE_CLAIM = "role";

    public static readonly TimeSpan TOKEN_LIFETIME = TimeSpan.FromHours(1);

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string SigningKey { get; init; }

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));

    public static JwtSettings FromConfiguration(IConfiguration configuration)
    {
        var signingKey = Require(configuration, SIGNING_KEY_KEY);

        if (signingKey.Length < MIN_SIGNING_KEY_LENGTH)
        {
            throw new InvalidOperationException($"{SIGNING_KEY_KEY} must be at least {MIN_SIGNING_KEY_LENGTH} characters.");
        }

        return new JwtSettings
        {
            Issuer = Require(configuration, ISSUER_KEY),
            Audience = Require(configuration, AUDIENCE_KEY),
            SigningKey = signingKey,
        };
    }

    private static string Require(IConfiguration configuration, string key)
    {
        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuration value {key} is required.");
        }

        return value;
    }
}
