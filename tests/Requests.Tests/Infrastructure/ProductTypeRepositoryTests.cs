using Requests.Domain.Products;
using Requests.Infrastructure.Products;

namespace Requests.Tests.Infrastructure;

public class ProductTypeRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Theory]
    [InlineData("water")]
    [InlineData("chips")]
    [InlineData("gum")]
    public async Task ExistsByName_is_true_for_a_seeded_type(string name)
    {
        Assert.True(await NewRepository().ExistsByNameAsync(name, CancellationToken.None));
    }

    [Theory]
    [InlineData("furniture")]
    [InlineData("drink")]
    [InlineData("Water")]
    public async Task ExistsByName_is_false_for_an_unknown_name_or_a_category(string name)
    {
        Assert.False(await NewRepository().ExistsByNameAsync(name, CancellationToken.None));
    }

    [Fact]
    public async Task GetAll_returns_every_type_ordered_by_code()
    {
        var all = await NewRepository().GetAllAsync(CancellationToken.None);

        Assert.Equal([1, 2, 3, 4, 5, 6, 7], all.Select(p => p.Code));
        Assert.Equal("water", all[0].Name);
        Assert.Equal(1, all[0].CategoryCode);
    }

    [Fact]
    public async Task Add_gets_the_next_code_from_the_database()
    {
        var fruit = new ProductType { Name = "fruit", CategoryCode = 2 };

        await NewRepository().AddAsync(fruit, CancellationToken.None);

        Assert.Equal(8, fruit.Code);
        var all = await NewRepository().GetAllAsync(CancellationToken.None);
        Assert.Equal("fruit", all[^1].Name);
        Assert.Equal(2, all[^1].CategoryCode);
    }

    private ProductTypeRepository NewRepository() => new(_database.CreateContext());
}
