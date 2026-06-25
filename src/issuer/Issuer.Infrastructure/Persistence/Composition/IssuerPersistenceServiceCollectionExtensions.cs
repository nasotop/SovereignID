using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Issuer.Infrastructure.Persistence.Composition;

public static class IssuerPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddIssuerPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));

        if (PersistenceServiceCollectionExtensions.UsesPostgresPersistence(configuration))
        {
            services.AddIssuerPostgresPersistence(configuration);
        }

        return services;
    }
}
