namespace Requests.Domain.Users;

public class User
{
    public required string Id { get; init; }

    public required string Username { get; init; }

    public required string Role { get; set; }
}
