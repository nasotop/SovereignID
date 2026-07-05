using Microsoft.EntityFrameworkCore;
using Reports.Application;
using Reports.Infrastructure.Metrics;
using Reports.Infrastructure.Persistence.Generated;
using Reports.Infrastructure.Persistence.Generated.Entities;
using Reports.Infrastructure.Persistence.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Reports.Infrastructure.Persistence.Composition;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddReportsPostgresPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ReportsDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection is required when Persistence:Provider is Postgres.");
            }

            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MapEnum<VerificationResult>("verification_result");
                npgsql.MapEnum<CredentialStatus>("credential_status");
            });
        });

        services.AddScoped<IReportCatalogReader, PostgresReportCatalog>();
        services.AddScoped<IMetricsSnapshotService, PostgresMetricsSnapshotService>();

        return services;
    }

    public static bool UsesPostgresPersistence(IConfiguration configuration) =>
        string.Equals(
            configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>()?.Provider
            ?? PersistenceProviders.InMemory,
            PersistenceProviders.Postgres,
            StringComparison.OrdinalIgnoreCase);
}
