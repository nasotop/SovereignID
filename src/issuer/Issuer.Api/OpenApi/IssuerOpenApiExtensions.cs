using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Issuer.Api.OpenApi;

internal static class IssuerOpenApiExtensions
{
    private const string DocumentName = "v1";

    public static IServiceCollection AddIssuerOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "SovereignID - Issuer API",
                    Version = "v1",
                    Description =
                        "Servicio emisor de Verifiable Credentials de la plataforma SovereignID. "
                        + "Vincula la wallet/DID emisor de una institucion y titulos emitidos a estudiantes "
                        + "usando la wallet primaria activa del estudiante. "
                        + "Los errores de negocio se devuelven como RFC 7807 Problem Details con un codigo estable "
                        + "en la extension `error`.",
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

    public static WebApplication MapIssuerOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("SovereignID - Issuer API")
                .WithTheme(ScalarTheme.Saturn);
        });

        return app;
    }
}
