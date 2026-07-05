using Auth.Infrastructure.Persistence.Generated;
using Auth.Infrastructure.Persistence.Generated.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infrastructure.Persistence.Composition;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddAuthPostgresPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required when Persistence:Provider is Postgres.");
        }

        services.AddDbContext<SovereignIdDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MapEnum<UserRole>("user_role");
                npgsql.MapEnum<GlobalUserRole>("global_user_role");
            }));

        return services;
    }

    public static bool UsesPostgresPersistence(IConfiguration configuration) =>
        string.Equals(
            configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>()?.Provider
            ?? PersistenceProviders.InMemory,
            PersistenceProviders.Postgres,
            StringComparison.OrdinalIgnoreCase);
}
