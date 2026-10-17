using Microsoft.EntityFrameworkCore;
using Requests.Infrastructure.Persistence;

namespace Requests.Tests.Infrastructure;

/// <summary>A fresh, migrated SQLite file in the temp folder, deleted when disposed.</summary>
public class TestDatabase : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"vending-test-{Guid.NewGuid()}.db");

    public TestDatabase()
    {
        using var db = CreateContext();
        db.Database.Migrate();
    }

    /// <summary>No pooling, so no connection keeps the file open after a context is disposed.</summary>
    public string ConnectionString => $"Data Source={_path};Pooling=False";

    /// <summary>A new context on the same database, like a new request would get.</summary>
    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(ConnectionString).Options);

    public void Dispose()
    {
        File.Delete(_path);
        GC.SuppressFinalize(this);
    }
}
