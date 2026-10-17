using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Requests.Infrastructure.Persistence;

namespace Requests.Tests.Infrastructure;

/// <summary>A fresh in-memory SQLite database that lives until disposed.</summary>
public class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestDatabase()
    {
        _connection.Open();

        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    /// <summary>A new context on the same database, like a new request would get.</summary>
    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
