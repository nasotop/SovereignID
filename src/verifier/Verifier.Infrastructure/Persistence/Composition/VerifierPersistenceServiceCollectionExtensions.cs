using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Verifier.Infrastructure.Persistence.Composition;

public static class VerifierPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddVerifierPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));

        if (PersistenceServiceCollectionExtensions.UsesPostgresPersistence(configuration))
        {
            services.AddVerifierPostgresPersistence(configuration);
        }

        return services;
    }
}
