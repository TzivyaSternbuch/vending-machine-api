using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Requests.Domain.Machines;

namespace Requests.Infrastructure.Persistence;

/// <summary>EF Core context backed by in-memory SQLite.</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<VendingMachine> Machines => Set<VendingMachine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite stores dates as text without a kind, so reads come back as Unspecified; every date we store is UTC.
        var utc = new ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        modelBuilder.Entity<VendingMachine>(machine =>
        {
            machine.HasKey(m => m.Id);
            machine.OwnsOne(m => m.Location);
            machine.Property(m => m.CreatedAt).HasConversion(utc);
            machine.Property(m => m.UpdatedAt).HasConversion(utc);

            // Shelf has no fields yet; it gets a table when the shelves feature lands.
            machine.Ignore(m => m.Shelves);
        });
    }
}
