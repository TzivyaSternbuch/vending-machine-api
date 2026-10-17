using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Requests.Application.Auth;
using Requests.Application.Machines;
using Requests.Infrastructure.Auth;
using Requests.Infrastructure.Machines;
using Requests.Infrastructure.Persistence;
using Requests.Infrastructure.Users;

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
        services.AddScoped<IUserRepository, UserRepository>();

        // Read when first resolved, not here, so test hosts can override the configuration.
        services.AddSingleton(provider => JwtSettings.FromConfiguration(provider.GetRequiredService<IConfiguration>()));
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

        return services;
    }

    /// <summary>Creates the schema in the in-memory database and adds the test users. Call once at startup.</summary>
    public static void InitializeDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        UserSeed.Seed(db);
    }
}
