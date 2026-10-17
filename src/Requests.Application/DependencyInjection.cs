using Microsoft.Extensions.DependencyInjection;

namespace Requests.Application;

public static class DependencyInjection
{
    /// <summary>Registers use cases. Empty until the first feature lands.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services) => services;
}
