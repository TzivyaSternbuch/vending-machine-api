using Requests.Api.Diagnostics;
using Requests.Api.Errors;
using Requests.Application;
using Requests.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure();

builder.Services.AddControllers();

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ErrorMapping.AddCodeAndDefaults);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Bad JSON or parameter binding throws BadHttpRequestException in every environment,
// so GlobalExceptionHandler handles it the same way everywhere.
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapControllers();

if (app.Environment.IsEnvironment(DiagnosticsEndpoints.EnvironmentName))
{
    app.MapDiagnosticsEndpoints();
}

app.Run();

public partial class Program; // lets tests reference the API
