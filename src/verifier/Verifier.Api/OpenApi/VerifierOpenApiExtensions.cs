using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Verifier.Api.Models;

namespace Verifier.Api.OpenApi;

internal static class VerifierOpenApiExtensions
{
    private const string DocumentName = "v1";

    private static readonly string[] VerificationResultWireValues =
    [
        "valid",
        "revoked",
        "expired",
        "not_found",
    ];

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
                        + "Expone la verificación pública de credenciales (`POST /verifications`) y un health-check (`GET /health`). "
                        + "Los veredictos de negocio (válida/revocada/expirada/inexistente) se devuelven con `200` y el campo `result`; "
                        + "los errores de protocolo se devuelven como RFC 7807 Problem Details con un código estable en la extensión `error`.",
                    Contact = new OpenApiContact
                    {
                        Name = "SovereignID",
                        Url = new Uri("https://github.com/nasotop/SovereignID")
                    }
                };

                return Task.CompletedTask;
            });

            options.AddSchemaTransformer((schema, context, _) =>
            {
                if (context.JsonTypeInfo.Type != typeof(VerificationResponse))
                {
                    return Task.CompletedTask;
                }

                schema.Properties!["result"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Enum = VerificationResultWireValues
                        .Select(value => (JsonNode)JsonValue.Create(value))
                        .ToList(),
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
