using System.Text.Json.Nodes;
using Bff.Api.Models;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Bff.Api.OpenApi;

internal static class BffOpenApiExtensions
{
    private const string DocumentName = "v1";

    private static readonly string[] VerificationResultWireValues =
    [
        "valid",
        "revoked",
        "expired",
        "not_found",
    ];

    private static readonly string[] CredentialStatusWireValues =
    [
        "active",
        "revoked",
        "expired",
    ];

    public static IServiceCollection AddBffOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new()
                {
                    Title = "SovereignID · BFF API",
                    Version = "v1",
                    Description =
                        "Backend-for-Frontend de SovereignID. Expone al portal web un contrato HTTP pass-through "
                        + "hacia los microservicios internos (verifier, issuer, academy, identity) mediante clientes Kiota. "
                        + "El navegador accede vía prefijo `/api/` (nginx strip). Auth SIWE permanece directo en `/auth/`.",
                    Contact = new()
                    {
                        Name = "SovereignID",
                        Url = new Uri("https://github.com/nasotop/SovereignID"),
                    },
                };

                return Task.CompletedTask;
            });

            options.AddSchemaTransformer((schema, context, _) =>
            {
                if (context.JsonTypeInfo.Type == typeof(VerificationResponse)
                    && schema.Properties is not null
                    && schema.Properties.ContainsKey("result"))
                {
                    schema.Properties["result"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Enum = VerificationResultWireValues
                            .Select(value => (JsonNode)JsonValue.Create(value))
                            .ToList(),
                    };
                }

                if ((context.JsonTypeInfo.Type == typeof(HolderCredentialSummary)
                     || context.JsonTypeInfo.Type == typeof(HolderCredentialDetail))
                    && schema.Properties is not null
                    && schema.Properties.ContainsKey("status"))
                {
                    schema.Properties["status"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Enum = CredentialStatusWireValues
                            .Select(value => (JsonNode)JsonValue.Create(value))
                            .ToList(),
                    };
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static WebApplication MapBffOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("SovereignID · BFF API")
                .WithTheme(ScalarTheme.Mars);
        });

        return app;
    }
}
