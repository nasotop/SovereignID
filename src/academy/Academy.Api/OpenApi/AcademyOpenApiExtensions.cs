using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Academy.Api.OpenApi;

internal static class AcademyOpenApiExtensions
{
    private const string DocumentName = "v1";

    public static IServiceCollection AddAcademyOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "SovereignID - Academy API",
                    Version = "v1",
                    Description =
                        "Servicio academico de la plataforma SovereignID. "
                        + "Gestiona instituciones, carreras, estudiantes e invitaciones para vincular wallets MetaMask existentes. "
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

    public static WebApplication MapAcademyOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("SovereignID - Academy API")
                .WithTheme(ScalarTheme.Saturn);
        });

        return app;
    }
}

