using Microsoft.Extensions.DependencyInjection;
using Requests.Application.Machines;
using Requests.Infrastructure.Machines;

namespace Requests.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers implementations of Application interfaces.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Singleton: the machines live in this one instance's dictionary. Scoped or transient would
        // create a new, empty store for every request, so a machine created by POST would be gone on GET.
        services.AddSingleton<IMachineRepository, InMemoryMachineRepository>();

        return services;
    }
}
