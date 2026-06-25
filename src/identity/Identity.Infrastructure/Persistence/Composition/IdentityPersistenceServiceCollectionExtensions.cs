using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure.Persistence.Composition;

public static class IdentityPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));

        if (PersistenceServiceCollectionExtensions.UsesPostgresPersistence(configuration))
        {
            services.AddIdentityPostgresPersistence(configuration);
        }

        return services;
    }
}
