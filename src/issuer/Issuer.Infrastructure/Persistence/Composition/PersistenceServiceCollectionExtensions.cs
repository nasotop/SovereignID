using Issuer.Application;
using Issuer.Infrastructure.Persistence.Generated;
using Issuer.Infrastructure.Persistence.Generated.Entities;
using Issuer.Infrastructure.Persistence.Stores;
using Issuer.Infrastructure.Persistence.Stores.CredentialStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Issuer.Infrastructure.Persistence.Composition;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddIssuerPostgresPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<IssuerDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection is required when Persistence:Provider is Postgres.");
            }

            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MapEnum<CredentialStatus>("credential_status");
                npgsql.MapEnum<WalletStatus>("wallet_status");
            });
        });

        services.AddScoped<ITitleIssuerRepository, PostgresTitleIssuerRepository>();
        services.AddScoped<ICredentialReadStore, EfCredentialReadStore>();

        services.AddDbContext<IssuerDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MapEnum<WalletStatus>("wallet_status");
                npgsql.MapEnum<CredentialStatus>("credential_status");
            });
        });
        services.AddScoped<ITitleIssuerRepository, PostgresTitleIssuerRepository>();

        return services;
    }

    public static bool UsesPostgresPersistence(IConfiguration configuration) =>
        string.Equals(
            configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>()?.Provider
            ?? PersistenceProviders.InMemory,
            PersistenceProviders.Postgres,
            StringComparison.OrdinalIgnoreCase);
}
