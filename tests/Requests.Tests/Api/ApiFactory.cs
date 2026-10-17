using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Requests.Tests.Infrastructure;

namespace Requests.Tests.Api;

/// <summary>Runs the API in memory, on its own temporary SQLite file that is deleted with the factory.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly TestDatabase _database = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _database.ConnectionString);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _database.Dispose();
        }
    }
}
