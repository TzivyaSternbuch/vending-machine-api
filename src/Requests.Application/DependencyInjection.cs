using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Requests.Application.Machines;
using Requests.Application.Products;

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
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<MachineValidator>();
        services.AddScoped<MachineService>();
        services.AddScoped<ProductCategoryValidator>();
        services.AddScoped<ProductCategoryService>();
        services.AddScoped<ProductTypeValidator>();
        services.AddScoped<ProductTypeService>();

        return services;
    }
}
