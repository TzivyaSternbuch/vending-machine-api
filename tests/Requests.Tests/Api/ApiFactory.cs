using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Requests.Infrastructure.Auth;

namespace Requests.Tests.Api;

/// <summary>Runs the API in memory, with its own in-memory database and the test token settings.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(JwtSettings.ISSUER_KEY, TestTokens.ISSUER);
        builder.UseSetting(JwtSettings.AUDIENCE_KEY, TestTokens.AUDIENCE);
        builder.UseSetting(JwtSettings.SIGNING_KEY_KEY, TestTokens.SIGNING_KEY);
    }
}
