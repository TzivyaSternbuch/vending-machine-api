using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Requests.Application.Machines;
using Requests.Infrastructure.Machines;
using Requests.Infrastructure.Persistence;

namespace Requests.Infrastructure;

public static class DependencyInjection
{
    private const string IN_MEMORY_CONNECTION_STRING = "DataSource=:memory:";

    /// <summary>Registers implementations of Application interfaces and the in-memory SQLite database.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(_ =>
        {
            var connection = new SqliteConnection(IN_MEMORY_CONNECTION_STRING);
            connection.Open();
            return connection;
        });

        services.AddDbContext<AppDbContext>((provider, options) =>
            options.UseSqlite(provider.GetRequiredService<SqliteConnection>()));

        services.AddScoped<IMachineRepository, MachineRepository>();

        return services;
    }

    /// <summary>Creates the schema in the in-memory database. Call once at startup.</summary>
    public static void InitializeDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }
}
