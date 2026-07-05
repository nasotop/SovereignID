using Reports.Application;
using Reports.Infrastructure.Metrics;
using Reports.Infrastructure.Persistence.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Reports.Infrastructure.Persistence.Composition;

public static class ReportsPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddReportsPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));

        if (PersistenceServiceCollectionExtensions.UsesPostgresPersistence(configuration))
        {
            services.AddReportsPostgresPersistence(configuration);
        }
        else
        {
            services.AddSingleton<InMemoryReportData>(sp =>
                InMemoryReportDataSeed.CreateDefault(sp.GetRequiredService<TimeProvider>()));
            services.AddSingleton<IReportCatalogReader, InMemoryReportCatalog>();
            services.AddSingleton<IMetricsSnapshotService, InMemoryMetricsSnapshotService>();
        }

        return services;
    }
}
