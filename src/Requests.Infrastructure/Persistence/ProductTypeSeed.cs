using Requests.Domain.Products;

namespace Requests.Infrastructure.Persistence;

/// <summary>
/// Categories and product types written by the seed migration. Their codes are fixed here so they are the
/// same in every database; rows added later through the API get the next code from the database.
/// </summary>
public static class ProductTypeSeed
{
    public const int DRINK = 1;

    public const int SNACK = 2;

    public const int CANDY = 3;

    public static readonly ProductCategory[] ProductCategories =
    [
        new ProductCategory { Code = DRINK, Name = "drink" },
        new ProductCategory { Code = SNACK, Name = "snack" },
        new ProductCategory { Code = CANDY, Name = "candy" },
    ];

    public static readonly ProductType[] ProductTypes =
    [
        new ProductType { Code = 1, Name = "water", CategoryCode = DRINK },
        new ProductType { Code = 2, Name = "cola", CategoryCode = DRINK },
        new ProductType { Code = 3, Name = "juice", CategoryCode = DRINK },
        new ProductType { Code = 4, Name = "chips", CategoryCode = SNACK },
        new ProductType { Code = 5, Name = "pretzels", CategoryCode = SNACK },
        new ProductType { Code = 6, Name = "chocolate", CategoryCode = CANDY },
        new ProductType { Code = 7, Name = "gum", CategoryCode = CANDY },
    ];
}
