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
        "integrity_failed",
    ];

    private static readonly string[] ValidationSourceWireValues =
    [
        "on_chain",
        "bd_fallback_inconclusive",
        "bd_fallback_rejected",
        "not_evaluated",
    ];

    private static readonly string[] RevocationSourceWireValues =
    [
        "bd",
        "on_chain",
        "both",
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
                        + "Los veredictos de negocio (válida/revocada/expirada/inexistente/integridad fallida) se devuelven con `200` y el campo `result`; "
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
                if (context.JsonTypeInfo.Type == typeof(VerificationResponse))
                {
                    schema.Properties!["result"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Enum = VerificationResultWireValues
                            .Select(value => (JsonNode)JsonValue.Create(value))
                            .ToList(),
                    };
                }

                if (context.JsonTypeInfo.Type == typeof(VerificationChecksResponse))
                {
                    schema.Properties!["validationSource"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Null | JsonSchemaType.String,
                        Enum = ValidationSourceWireValues
                            .Select(value => (JsonNode)JsonValue.Create(value))
                            .ToList(),
                    };

                    schema.Properties!["revocationSource"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Null | JsonSchemaType.String,
                        Enum = RevocationSourceWireValues
                            .Select(value => (JsonNode)JsonValue.Create(value))
                            .ToList(),
                    };
                }

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                if (context.Description.HttpMethod is not null
                    && HttpMethods.IsPost(context.Description.HttpMethod)
                    && string.Equals(context.Description.RelativePath, "verifications", StringComparison.OrdinalIgnoreCase))
                {
                    operation.Responses ??= new OpenApiResponses();
                    operation.Responses["429"] = new OpenApiResponse
                    {
                        Description = "Too Many Requests — Problem Details with `error = rate_limit_exceeded`."
                    };
                }

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
