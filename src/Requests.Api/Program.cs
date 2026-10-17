using Requests.Api.Errors;
using Requests.Application;
using Requests.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = ErrorResponses.InvalidModelState;
        o.SuppressMapClientErrors = true;
    });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Bad JSON or parameter binding throws BadHttpRequestException in every environment,
// so GlobalExceptionHandler handles it the same way everywhere.
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);

var app = builder.Build();

app.Services.InitializeDatabase();

app.UseExceptionHandler(_ => { });
app.UseStatusCodePages(ErrorResponses.WriteStatusCodePageAsync);

app.MapControllers();

app.Run();

public partial class Program; // lets tests reference the API
