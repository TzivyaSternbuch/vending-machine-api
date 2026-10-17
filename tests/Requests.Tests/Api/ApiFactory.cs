using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Requests.Tests.Infrastructure;

namespace Requests.Tests.Api;

/// <summary>Runs the API in memory, on its own temporary SQLite file that is deleted with the factory.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly TestDatabase _database;

    private readonly bool _ownsDatabase;

    public ApiFactory()
        : this(new TestDatabase(), ownsDatabase: true)
    {
    }

    // For restart tests: the caller keeps the file, so a second factory can open it.
    // A method, not a constructor: xUnit class fixtures must have exactly one public constructor.
    public static ApiFactory OnDatabase(TestDatabase database) => new(database, ownsDatabase: false);

    private ApiFactory(TestDatabase database, bool ownsDatabase)
    {
        _database = database;
        _ownsDatabase = ownsDatabase;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _database.ConnectionString);
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
