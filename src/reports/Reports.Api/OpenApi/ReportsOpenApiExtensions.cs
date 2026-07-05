using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Reports.Api.OpenApi;

internal static class ReportsOpenApiExtensions
{
    private const string DocumentName = "v1";

    public static IServiceCollection AddReportsOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "SovereignID · Reports API",
                    Version = "v1",
                    Description =
                        "Servicio de reportería de solo lectura de la plataforma SovereignID. "
                        + "Expone KPIs institution-scoped (admin, issuer, viewer) y platform-scoped (platform_admin). "
                        + "Los errores de negocio se devuelven como RFC 7807 Problem Details con un código estable "
                        + "en la extensión ``error`` (p. ej. ``invalid_report_period``, ``institution_not_found``).",
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

    public static WebApplication MapReportsOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("SovereignID · Reports API")
                .WithTheme(ScalarTheme.Purple);
        });

        return app;
    }
}
