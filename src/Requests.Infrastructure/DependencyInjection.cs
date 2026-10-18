using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Requests.Application.Auth;
using Requests.Application.Machines;
using Requests.Application.Persistence;
using Requests.Application.Products;
using Requests.Infrastructure.Auth;
using Requests.Infrastructure.Machines;
using Requests.Infrastructure.Persistence;
using Requests.Infrastructure.Products;
using Requests.Infrastructure.Users;

namespace Requests.Infrastructure;

public static class DependencyInjection
{
    public const string CONNECTION_STRING_NAME = "Default";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((provider, options) =>
            options.UseSqlite(provider.GetRequiredService<IConfiguration>().GetConnectionString(CONNECTION_STRING_NAME)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMachineRepository, MachineRepository>();
        services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
        services.AddScoped<IProductTypeRepository, ProductTypeRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Read when first resolved, not here, so test hosts can override the configuration.
        services.AddSingleton(provider => JwtSettings.FromConfiguration(provider.GetRequiredService<IConfiguration>()));
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

        return services;
    }

    // The test users come from a migration too, like the product types.
    public static void InitializeDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    }
}
