using Microsoft.OpenApi.Models;

namespace Requests.Api.Swagger;

/// <summary>
/// Swagger UI at /swagger, in every environment. Its Authorize button takes the accessToken from
/// POST /auth/login and sends it as "Authorization: Bearer ..." on the endpoints that need it.
/// </summary>
public static class SwaggerSetup
{
    public const string BEARER_SCHEME_ID = "Bearer";

    public static IServiceCollection AddSwaggerWithBearer(this IServiceCollection services)
    {
        services.AddSwaggerGen(o =>
        {
            o.AddSecurityDefinition(BEARER_SCHEME_ID, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste the accessToken from POST /auth/login.",
            });

            o.OperationFilter<BearerRequirementFilter>();
        });

        return services;
    }
}
