using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Requests.Domain.Machines;
using Requests.Domain.Products;

namespace Requests.Infrastructure.Persistence;

/// <summary>EF Core context backed by a SQLite file. Schema changes go through migrations.</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<VendingMachine> Machines => Set<VendingMachine>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    public DbSet<ProductType> ProductTypes => Set<ProductType>();

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

        modelBuilder.Entity<ProductCategory>(category =>
        {
            category.HasKey(c => c.Code);
            category.Property(c => c.Code).ValueGeneratedNever();
            category.HasIndex(c => c.Name).IsUnique();
            category.HasData(ProductTypeSeed.ProductCategories);
        });

        modelBuilder.Entity<ProductType>(productType =>
        {
            productType.HasKey(p => p.Code);
            productType.Property(p => p.Code).ValueGeneratedNever();
            productType.HasIndex(p => p.Name).IsUnique();
            productType.HasOne<ProductCategory>()
                .WithMany()
                .HasForeignKey(p => p.CategoryCode)
                .OnDelete(DeleteBehavior.Restrict);
            productType.HasData(ProductTypeSeed.ProductTypes);
        });
    }
}
