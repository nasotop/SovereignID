using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Verifier.Infrastructure.Persistence.Generated;
using Verifier.Infrastructure.Persistence.Generated.Entities;

namespace Verifier.Infrastructure.Persistence.Composition;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddVerifierPostgresPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // The connection string is resolved at runtime (not at registration) so that test hosts
        // and other late configuration sources (e.g. WebApplicationFactory) can override it.
        services.AddDbContext<VerifierDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
            }

            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MapEnum<CredentialStatus>("credential_status");
                npgsql.MapEnum<VerificationResult>("verification_result");
            });
        });

        return services;
    }
}
