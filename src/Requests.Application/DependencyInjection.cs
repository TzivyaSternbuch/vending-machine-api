using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Requests.Application.Machines;
using Requests.Application.Products;
using Requests.Application.Shelves;

namespace Requests.Application;

public static class DependencyInjection
{
    /// <summary>Registers use cases.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(new MachineSettings
        {
            MaxShelvesLimit = configuration.GetValue(MachineSettings.MAX_SHELVES_LIMIT_KEY, MachineSettings.DEFAULT_MAX_SHELVES_LIMIT),
        });
        services.AddSingleton(new ShelfSettings
        {
            MaxShelfCapacity = configuration.GetValue(ShelfSettings.MAX_SHELF_CAPACITY_KEY, ShelfSettings.DEFAULT_MAX_SHELF_CAPACITY),
        });
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<MachineValidator>();
        services.AddScoped<MachineService>();
        services.AddScoped<ProductCategoryValidator>();
        services.AddScoped<ProductCategoryService>();
        services.AddScoped<ProductTypeValidator>();
        services.AddScoped<ProductTypeService>();
        services.AddScoped<AddShelfRequestValidator>();

        return services;
    }
}
