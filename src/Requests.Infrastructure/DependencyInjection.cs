using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Requests.Application.Machines;
using Requests.Application.Persistence;
using Requests.Application.Products;
using Requests.Infrastructure.Machines;
using Requests.Infrastructure.Persistence;
using Requests.Infrastructure.Products;

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

        return services;
    }

    public static void InitializeDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    }
}
