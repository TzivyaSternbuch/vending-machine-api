using Microsoft.AspNetCore.Mvc.Testing;

namespace Requests.Tests.Api;

/// <summary>Runs the API in memory, with its own in-memory database.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
}
