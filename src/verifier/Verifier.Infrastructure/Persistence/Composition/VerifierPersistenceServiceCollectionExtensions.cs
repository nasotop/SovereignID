using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Verifier.Application;
using Verifier.Infrastructure.Persistence.Stores.CredentialStore;
using Verifier.Infrastructure.Persistence.Stores.VerificationLogStore;

namespace Verifier.Infrastructure.Persistence.Composition;

public static class VerifierPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddVerifierPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddVerifierPostgresPersistence(configuration);

        services.AddScoped<ICredentialReadStore, EfCredentialReadStore>();
        services.AddScoped<IVerificationLogStore, EfVerificationLogStore>();

        return services;
    }
}
