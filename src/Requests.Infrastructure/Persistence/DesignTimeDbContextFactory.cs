using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Requests.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef</c> build the context without starting the API. Used only to create migrations.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DESIGN_TIME_CONNECTION_STRING = "Data Source=vending.db";

    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(DESIGN_TIME_CONNECTION_STRING).Options);
}
