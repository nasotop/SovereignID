using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Identity.Api.OpenApi;

internal static class IdentityOpenApiExtensions
{
    private const string DocumentName = "v1";

    public static IServiceCollection AddIdentityOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "SovereignID · Identity API",
                    Version = "v1",
                    Description =
                        "Servicio de identidad de la plataforma SovereignID. "
                        + "Expone un health-check (`GET /health`); los endpoints de negocio se añadirán en próximas iteraciones. "
                        + "Los errores de negocio se devuelven como RFC 7807 Problem Details con un código estable "
                        + "en la extensión `error`.",
                    Contact = new OpenApiContact
                    {
                        Name = "SovereignID",
                        Url = new Uri("https://github.com/nasotop/SovereignID")
                    }
                };

                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static WebApplication MapIdentityOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("SovereignID · Identity API")
                .WithTheme(ScalarTheme.BluePlanet);
        });

        return app;
    }
}
