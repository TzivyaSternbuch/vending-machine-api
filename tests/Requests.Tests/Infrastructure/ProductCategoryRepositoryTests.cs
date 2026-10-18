using Requests.Domain.Products;
using Requests.Infrastructure.Products;

namespace Requests.Tests.Infrastructure;

public class ProductCategoryRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(0, false)]
    public async Task Exists_checks_the_code(int code, bool expected)
    {
        Assert.Equal(expected, await NewRepository().ExistsAsync(code, CancellationToken.None));
    }

    [Theory]
    [InlineData("drink", true)]
    [InlineData("water", false)]
    [InlineData("Drink", false)]
    public async Task ExistsByName_checks_the_exact_name(string name, bool expected)
    {
        Assert.Equal(expected, await NewRepository().ExistsByNameAsync(name, CancellationToken.None));
    }

    [Fact]
    public async Task GetAll_returns_every_category_ordered_by_code()
    {
        var all = await NewRepository().GetAllAsync(CancellationToken.None);

        Assert.Equal(["drink", "snack", "candy"], all.Select(c => c.Name));
        Assert.Equal([1, 2, 3], all.Select(c => c.Code));
    }

    [Fact]
    public async Task Add_gets_the_next_code_from_the_database()
    {
        var fruit = new ProductCategory { Name = "fruit" };

        await NewRepository().AddAsync(fruit, CancellationToken.None);

        Assert.Equal(4, fruit.Code);
        Assert.True(await NewRepository().ExistsAsync(4, CancellationToken.None));
    }

    private ProductCategoryRepository NewRepository() => new(_database.CreateContext());
}
