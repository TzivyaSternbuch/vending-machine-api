using Requests.Application.Exceptions;

namespace Requests.Application.Auth;

/// <summary>Test login by username only: there are no passwords, any seeded username gets a token.</summary>
public class AuthService(IUserRepository users, ITokenIssuer tokenIssuer)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ValidationException("username", "is required");
        }

        var user = await users.FindByUsernameAsync(request.Username, cancellationToken)
            ?? throw new UnauthorizedException($"Unknown user '{request.Username}'.");

        return tokenIssuer.Issue(user);
    }
}
