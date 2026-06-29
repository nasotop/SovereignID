using Academy.Application;
using Academy.Infrastructure.Persistence.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Academy.Infrastructure.Persistence.Composition;

public static class AcademyPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddAcademyPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));

        if (PersistenceServiceCollectionExtensions.UsesPostgresPersistence(configuration))
        {
            services.AddAcademyPostgresPersistence(configuration);
        }
        else
        {
            services.AddSingleton<IAcademyRepository, InMemoryAcademyRepository>();
        }

        return services;
    }
}

