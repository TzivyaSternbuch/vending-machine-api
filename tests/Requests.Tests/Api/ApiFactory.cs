using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Requests.Api.Diagnostics;

namespace Requests.Tests.Api;

/// <summary>Runs the API in memory in the Testing environment, which maps the diagnostics endpoints.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment(DiagnosticsEndpoints.EnvironmentName);
}
