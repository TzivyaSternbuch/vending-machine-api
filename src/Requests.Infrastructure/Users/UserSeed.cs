using Requests.Domain.Users;
using Requests.Infrastructure.Persistence;

namespace Requests.Infrastructure.Users;

/// <summary>The two test accounts. The README lists them; there is no way to add users yet.</summary>
public static class UserSeed
{
    public const string ADMIN_USERNAME = "admin";

    public const string USER_USERNAME = "user";

    public static void Seed(AppDbContext db)
    {
        db.Users.Add(Create(ADMIN_USERNAME, Roles.ADMIN));
        db.Users.Add(Create(USER_USERNAME, Roles.USER));
        db.SaveChanges();
    }

    private static User Create(string username, string role) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Username = username,
        Role = role,
    };
}
