using Requests.Domain.Products;
using Requests.Infrastructure.Products;

namespace Requests.Tests.Infrastructure;

public class ProductTypeRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Theory]
    [InlineData("water", 1, 1)]
    [InlineData("chips", 4, 2)]
    [InlineData("gum", 7, 3)]
    public async Task FindByName_returns_a_seeded_type(string name, int expectedCode, int expectedCategoryCode)
    {
        var found = await NewRepository().FindByNameAsync(name, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(expectedCode, found.Code);
        Assert.Equal(name, found.Name);
        Assert.Equal(expectedCategoryCode, found.CategoryCode);
    }

    [Theory]
    [InlineData("furniture")]
    [InlineData("drink")]
    [InlineData("Water")]
    [InlineData("")]
    public async Task FindByName_returns_null_for_an_unknown_name_or_a_category(string name)
    {
        Assert.Null(await NewRepository().FindByNameAsync(name, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllNames_returns_every_type_in_alphabetical_order()
    {
        var names = await NewRepository().GetAllNamesAsync(CancellationToken.None);

        Assert.Equal(["chips", "chocolate", "cola", "gum", "juice", "pretzels", "water"], names);
    }

    [Fact]
    public async Task New_type_row_is_found_without_code_changes()
    {
        using (var db = _database.CreateContext())
        {
            db.ProductTypes.Add(new ProductType { Code = 8, Name = "fruit", CategoryCode = 2 });
            await db.SaveChangesAsync();
        }

        var repository = NewRepository();

        Assert.Equal(8, (await repository.FindByNameAsync("fruit", CancellationToken.None))?.Code);
        Assert.Contains("fruit", await repository.GetAllNamesAsync(CancellationToken.None));
    }

    private ProductTypeRepository NewRepository() => new(_database.CreateContext());
}
