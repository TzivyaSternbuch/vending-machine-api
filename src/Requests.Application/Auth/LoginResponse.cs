namespace Requests.Application.Auth;

public class LoginResponse
{
    public required string AccessToken { get; init; }

    public required DateTime ExpiresAt { get; init; }
}
