using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Auth.Api.OpenApi;

internal static class AuthOpenApiExtensions
{
    private const string DocumentName = "v1";

    public static IServiceCollection AddAuthOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "SovereignID · Auth API",
                    Version = "v1",
                    Description =
                        "Autenticación Sign-In with Ethereum (EIP-4361) para Ethereum Sepolia (chain ID 11155111). "
                        + "Emite un reto de un solo uso (`GET /auth/nonce`) y verifica un mensaje SIWE firmado "
                        + "para entregar un JWT de sesión (`POST /auth/verify`). "
                        + "Los errores de negocio se devuelven como RFC 7807 Problem Details con un código estable "
                        + "en la extensión `error` (p. ej. `nonce_expired`, `signature_mismatch`).",
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

    public static WebApplication MapAuthOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("SovereignID · Auth API")
                .WithTheme(ScalarTheme.Purple);
        });

        return app;
    }
}
