using Requests.Domain.Users;

namespace Requests.Infrastructure.Users;

/// <summary>
/// The two test accounts, written by a migration. Their ids are fixed so the database file keeps the same
/// users across restarts. The README lists them; there is no way to add users yet.
/// </summary>
public static class UserSeed
{
    public const string ADMIN_USERNAME = "admin";

    public const string USER_USERNAME = "user";

    public const string ADMIN_ID = "6f1c2a3e-8a4b-4d2e-9c71-0b5e3f2a1d01";

    public const string USER_ID = "6f1c2a3e-8a4b-4d2e-9c71-0b5e3f2a1d02";

    public static readonly User[] Users =
    [
        new User { Id = ADMIN_ID, Username = ADMIN_USERNAME, Role = Roles.ADMIN },
        new User { Id = USER_ID, Username = USER_USERNAME, Role = Roles.USER },
    ];
}
