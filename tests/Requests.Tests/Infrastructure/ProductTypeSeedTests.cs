using Microsoft.EntityFrameworkCore;
using Requests.Domain.Products;

namespace Requests.Tests.Infrastructure;

/// <summary>What the migrations leave in a new database.</summary>
public class ProductTypeSeedTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Theory]
    [InlineData(1, "drink")]
    [InlineData(2, "snack")]
    [InlineData(3, "candy")]
    public async Task Migration_seeds_each_category_with_its_code(int code, string name)
    {
        using var db = _database.CreateContext();

        var category = await db.ProductCategories.SingleAsync(c => c.Code == code);

        Assert.Equal(name, category.Name);
    }

    [Fact]
    public async Task Migration_seeds_three_categories()
    {
        using var db = _database.CreateContext();

        Assert.Equal(3, await db.ProductCategories.CountAsync());
    }

    [Theory]
    [InlineData(1, "water", "drink")]
    [InlineData(2, "cola", "drink")]
    [InlineData(3, "juice", "drink")]
    [InlineData(4, "chips", "snack")]
    [InlineData(5, "pretzels", "snack")]
    [InlineData(6, "chocolate", "candy")]
    [InlineData(7, "gum", "candy")]
    public async Task Migration_seeds_each_type_linked_to_its_category(int code, string name, string categoryName)
    {
        using var db = _database.CreateContext();

        var productType = await db.ProductTypes.SingleAsync(p => p.Code == code);
        var category = await db.ProductCategories.SingleAsync(c => c.Code == productType.CategoryCode);

        Assert.Equal(name, productType.Name);
        Assert.Equal(categoryName, category.Name);
    }

    [Fact]
    public async Task Migration_seeds_seven_types()
    {
        using var db = _database.CreateContext();

        Assert.Equal(7, await db.ProductTypes.CountAsync());
    }

    [Fact]
    public async Task Type_with_unknown_category_is_rejected_by_the_foreign_key()
    {
        using var db = _database.CreateContext();
        db.ProductTypes.Add(new ProductType { Code = 100, Name = "chair", CategoryCode = 99 });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Type_with_a_taken_name_is_rejected()
    {
        using var db = _database.CreateContext();
        db.ProductTypes.Add(new ProductType { Code = 100, Name = "water", CategoryCode = 1 });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Category_with_a_taken_name_is_rejected()
    {
        using var db = _database.CreateContext();
        db.ProductCategories.Add(new ProductCategory { Code = 100, Name = "drink" });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Category_used_by_a_type_cannot_be_deleted()
    {
        using var db = _database.CreateContext();
        db.ProductCategories.Remove(await db.ProductCategories.SingleAsync(c => c.Code == 1));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
