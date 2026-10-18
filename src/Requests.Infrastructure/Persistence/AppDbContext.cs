using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Requests.Domain.Machines;
using Requests.Domain.Products;
using Requests.Domain.Users;
using Requests.Infrastructure.Users;

namespace Requests.Infrastructure.Persistence;

/// <summary>EF Core context backed by a SQLite file. Schema changes go through migrations.</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<VendingMachine> Machines => Set<VendingMachine>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    public DbSet<ProductType> ProductTypes => Set<ProductType>();

    public DbSet<User> Users => Set<User>();

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
            machine.HasMany(m => m.Shelves)
                .WithOne()
                .HasForeignKey(s => s.MachineId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Shelf>(shelf =>
        {
            shelf.ToTable("Shelves", table =>
            {
                table.HasCheckConstraint("CK_Shelves_Price", "\"Price\" > 0");
                table.HasCheckConstraint("CK_Shelves_Quantity", "\"Quantity\" >= 0 AND \"Quantity\" <= \"Capacity\"");
            });
            shelf.HasKey(s => s.Id);
            shelf.HasOne<ProductType>()
                .WithMany()
                .HasForeignKey(s => s.ProductTypeCode)
                .OnDelete(DeleteBehavior.Restrict);
            shelf.Property(s => s.CreatedAt).HasConversion(utc);
            shelf.Property(s => s.UpdatedAt).HasConversion(utc);
        });

        modelBuilder.Entity<ProductCategory>(category =>
        {
            category.HasKey(c => c.Code);
            category.HasIndex(c => c.Name).IsUnique();
            category.HasData(ProductTypeSeed.ProductCategories);
        });

        modelBuilder.Entity<ProductType>(productType =>
        {
            productType.HasKey(p => p.Code);
            productType.HasIndex(p => p.Name).IsUnique();
            productType.HasOne<ProductCategory>()
                .WithMany()
                .HasForeignKey(p => p.CategoryCode)
                .OnDelete(DeleteBehavior.Restrict);
            productType.HasData(ProductTypeSeed.ProductTypes);
        });

        modelBuilder.Entity<User>(user =>
        {
            user.HasKey(u => u.Id);
            user.HasIndex(u => u.Username).IsUnique();
            user.HasData(UserSeed.Users);
        });
    }
}
