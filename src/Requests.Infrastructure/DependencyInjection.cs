using Microsoft.Extensions.DependencyInjection;

namespace Requests.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers implementations of Application interfaces. Empty until the first feature lands.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services) => services;
}
