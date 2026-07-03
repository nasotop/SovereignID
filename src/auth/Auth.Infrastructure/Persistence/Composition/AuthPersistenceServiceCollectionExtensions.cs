using Auth.Application;
using Auth.Infrastructure.Authorization;
using Auth.Infrastructure.Persistence.Stores.ChallengeStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infrastructure.Persistence.Composition;

public static class AuthPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddAuthPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));

        if (PersistenceServiceCollectionExtensions.UsesPostgresPersistence(configuration))
        {
            services.AddAuthPostgresPersistence(configuration);
            services.AddScoped<IChallengeStore, PostgresChallengeStore>();
            services.AddScoped<IUserAuthorizationResolver, PostgresUserAuthorizationResolver>();
        }
        else
        {
            services.AddSingleton<IChallengeStore, InMemoryChallengeStore>();
            services.AddSingleton<IUserAuthorizationResolver, InMemoryUserAuthorizationResolver>();
        }

        return services;
    }
}
