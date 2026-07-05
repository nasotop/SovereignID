using Reports.Application;
using Reports.Infrastructure.Metrics;
using Reports.Infrastructure.Persistence.Composition;
using Reports.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Reports.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReportsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ReportsOptions>(configuration.GetSection(ReportsOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IReportCatalog, ReportCatalog>();
        services.AddReportsPersistence(configuration);
        services.AddReportsJwtAuthentication(configuration);
        services.AddReportsAuthorization();
        services.AddHostedService<MetricsSnapshotJob>();

        return services;
    }

    public static void ValidateReportsConfiguration(this IHost host)
    {
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        if (PersistenceServiceCollectionExtensions.UsesPostgresPersistence(configuration)
            && string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required when Persistence:Provider is Postgres.");
        }
    }
}
