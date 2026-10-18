using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Requests.Infrastructure.Auth;
using Requests.Tests.Infrastructure;

namespace Requests.Tests.Api;

/// <summary>Runs the API in memory, on its own temporary SQLite file that is deleted with the factory, and the test token settings.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly TestDatabase _database;

    private readonly bool _ownsDatabase;

    public ApiFactory()
        : this(new TestDatabase(), ownsDatabase: true)
    {
    }

    private ApiFactory(TestDatabase database, bool ownsDatabase)
    {
        _database = database;
        _ownsDatabase = ownsDatabase;
    }

    // For restart tests: the caller keeps the file, so a second factory can open it.
    // A method, not a constructor: xUnit class fixtures must have exactly one public constructor.
    public static ApiFactory OnDatabase(TestDatabase database) => new(database, ownsDatabase: false);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _database.ConnectionString);
        builder.UseSetting(JwtSettings.ISSUER_KEY, TestTokens.ISSUER);
        builder.UseSetting(JwtSettings.AUDIENCE_KEY, TestTokens.AUDIENCE);
        builder.UseSetting(JwtSettings.SIGNING_KEY_KEY, TestTokens.SIGNING_KEY);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && _ownsDatabase)
        {
            _database.Dispose();
        }
    }
}
