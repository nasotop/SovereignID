using Issuer.Application;
using Issuer.Infrastructure.Persistence.Stores;
using Issuer.Infrastructure.Persistence.Stores.CredentialStore;
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
        else
        {
            services.AddSingleton<ITitleIssuerRepository, InMemoryTitleIssuerRepository>();
            services.AddSingleton<ICredentialReadStore, InMemoryCredentialReadStore>();
        }

        return services;
    }
}
