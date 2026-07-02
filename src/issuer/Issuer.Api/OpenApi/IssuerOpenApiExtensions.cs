using System.Text.Json.Nodes;
using Issuer.Application;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Issuer.Api.OpenApi;

internal static class IssuerOpenApiExtensions
{
    private const string DocumentName = "v1";
    private const string BearerAuthSchemeId = "bearerAuth";

    private static readonly string[] CredentialStatusWireValues =
    [
        "active",
        "revoked",
        "expired",
    ];

    private static readonly string[] SecuredHolderPaths =
    [
        "/issuer/holders/me/credentials",
        "/issuer/holders/me/credentials/{credentialId}",
        "/issuer/credentials/{credentialId}",
    ];

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

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[BearerAuthSchemeId] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT SIWE emitido por el servicio auth.",
                };

                ApplyBearerSecurityToHolderOperations(document);

                return Task.CompletedTask;
            });

            options.AddSchemaTransformer((schema, context, _) =>
            {
                if (context.JsonTypeInfo.Type != typeof(HolderCredentialSummary)
                    && context.JsonTypeInfo.Type != typeof(HolderCredentialDetail))
                {
                    return Task.CompletedTask;
                }

                if (schema.Properties is null || !schema.Properties.ContainsKey("status"))
                {
                    return Task.CompletedTask;
                }

                schema.Properties["status"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Enum = CredentialStatusWireValues
                        .Select(value => (JsonNode)JsonValue.Create(value))
                        .ToList(),
                };

                return Task.CompletedTask;
            });
        });

        return services;
    }

    private static void ApplyBearerSecurityToHolderOperations(OpenApiDocument document)
    {
        if (document.Paths is null)
        {
            return;
        }

        var requirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(BearerAuthSchemeId, document)] = [],
        };

        foreach (var pathKey in SecuredHolderPaths)
        {
            if (!document.Paths.TryGetValue(pathKey, out var pathItem)
                || pathItem.Operations is null)
            {
                continue;
            }

            foreach (var operation in pathItem.Operations.Values)
            {
                operation.Security = [requirement];
            }
        }
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
