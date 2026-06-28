using Academy.Application;
using Academy.Infrastructure.Persistence.Entities;
using Academy.Infrastructure.Persistence.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Academy.Infrastructure.Persistence.Composition;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddAcademyPostgresPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required when Persistence:Provider is Postgres.");
        }

        services.AddDbContext<AcademyDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MapEnum<UserRole>("user_role");
                npgsql.MapEnum<WalletStatus>("wallet_status");
            });
        });
        services.AddScoped<IAcademyRepository, PostgresAcademyRepository>();

        return services;
    }

    public static bool UsesPostgresPersistence(IConfiguration configuration) =>
        string.Equals(
            configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>()?.Provider
            ?? PersistenceProviders.InMemory,
            PersistenceProviders.Postgres,
            StringComparison.OrdinalIgnoreCase);
}

