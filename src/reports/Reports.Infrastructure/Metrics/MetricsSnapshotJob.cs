using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reports.Application;

namespace Reports.Infrastructure.Metrics;

internal sealed class MetricsSnapshotJob(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<ReportsOptions> options,
    ILogger<MetricsSnapshotJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = timeProvider.GetUtcNow();
            var nextRun = GetNextRunUtc(now, options.Value.SnapshotJobHourUtc);
            var delay = nextRun - now;
            if (delay > TimeSpan.Zero)
            {
                logger.LogInformation("Metrics snapshot job sleeping until {NextRunUtc}", nextRun);
                await Task.Delay(delay, stoppingToken);
            }

            var yesterday = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1);
            await RunSnapshotAsync(yesterday, stoppingToken);
        }
    }

    private async Task RunSnapshotAsync(DateOnly metricDate, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var snapshotService = scope.ServiceProvider.GetRequiredService<IMetricsSnapshotService>();
            await snapshotService.UpsertDailyMetricsAsync(metricDate, cancellationToken);
            logger.LogInformation("Metrics snapshot completed for {MetricDate}", metricDate);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Metrics snapshot failed for {MetricDate}", metricDate);
        }
    }

    private static DateTimeOffset GetNextRunUtc(DateTimeOffset now, int hourUtc)
    {
        var scheduled = new DateTimeOffset(now.Year, now.Month, now.Day, hourUtc, 0, 0, TimeSpan.Zero);
        return scheduled <= now ? scheduled.AddDays(1) : scheduled;
    }
}
