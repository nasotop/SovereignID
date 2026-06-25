using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Verifier.Api.OpenApi;

internal static class VerifierOpenApiExtensions
{
    private const string DocumentName = "v1";

    public static IServiceCollection AddVerifierOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "SovereignID · Verifier API",
                    Version = "v1",
                    Description =
                        "Servicio verificador de Verifiable Credentials de la plataforma SovereignID. "
                        + "Expone un health-check (`GET /health`); los endpoints de verificación se añadirán en próximas iteraciones. "
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

    public static WebApplication MapVerifierOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("SovereignID · Verifier API")
                .WithTheme(ScalarTheme.Mars);
        });

        return app;
    }
}
