using Microsoft.EntityFrameworkCore;
using Reports.Application;
using Reports.Domain;
using Reports.Infrastructure.Persistence.Generated;
using Reports.Infrastructure.Persistence.Generated.Entities;

namespace Reports.Infrastructure.Persistence.Stores;

internal sealed class PostgresReportCatalog(ReportsDbContext db, TimeProvider timeProvider) : IReportCatalogReader
{
    public Task<bool> InstitutionExistsAsync(Guid institutionId, CancellationToken cancellationToken = default) =>
        db.Institutions.AsNoTracking().AnyAsync(i => i.Id == institutionId, cancellationToken);

    public Task<TimeSeriesReport> GetInstitutionTimeSeriesAsync(
        InstitutionReportId reportId,
        Guid institutionId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default) =>
        reportId switch
        {
            InstitutionReportId.CredentialsIssued => BuildTimeSeriesAsync(
                institutionId,
                from,
                to,
                row => row.CredentialsIssued,
                CountCredentialsIssuedLiveAsync,
                cancellationToken),
            InstitutionReportId.CredentialReads => BuildTimeSeriesAsync(
                institutionId,
                from,
                to,
                row => row.VerificationsTotal,
                CountVerificationsLiveAsync,
                cancellationToken),
            InstitutionReportId.CredentialsRevoked => BuildTimeSeriesAsync(
                institutionId,
                from,
                to,
                row => row.CredentialsRevoked,
                CountCredentialsRevokedLiveAsync,
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(reportId), reportId, "Not a time-series institution report.")
        };

    public async Task<CredentialsPerStudentReport> GetInstitutionCredentialsPerStudentAsync(
        Guid institutionId,
        DateOnly asOf,
        CancellationToken cancellationToken = default)
    {
        var asOfEnd = asOf.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var totalStudents = await db.Students.AsNoTracking()
            .CountAsync(
                s => s.InstitutionId == institutionId
                     && s.IsActive
                     && s.CreatedAt < asOfEnd,
                cancellationToken);

        var totalCredentials = await db.Credentials.AsNoTracking()
            .CountAsync(
                c => c.InstitutionId == institutionId
                     && c.IssuedAt < asOfEnd,
                cancellationToken);

        var average = totalStudents == 0 ? 0d : Math.Round((double)totalCredentials / totalStudents, 1);

        return new CredentialsPerStudentReport(asOf, totalStudents, totalCredentials, average);
    }

    public Task<VerificationOutcomesReport> GetInstitutionVerificationOutcomesAsync(
        Guid institutionId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default) =>
        BuildVerificationOutcomesAsync(institutionId, from, to, cancellationToken);

    public async Task<PlatformInstitutionRanking> GetPlatformCredentialsByInstitutionAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var institutions = await db.Institutions.AsNoTracking()
            .Where(i => i.IsActive)
            .Select(i => new { i.Id, i.DisplayName })
            .ToListAsync(cancellationToken);

        var items = new List<PlatformRankingItem>();
        ReportDataSource? aggregateSource = null;

        foreach (var institution in institutions)
        {
            var series = await GetInstitutionTimeSeriesAsync(
                InstitutionReportId.CredentialsIssued,
                institution.Id,
                from,
                to,
                cancellationToken);

            items.Add(new PlatformRankingItem(institution.Id, institution.DisplayName, series.Total));
            aggregateSource = aggregateSource is null
                ? series.Source
                : aggregateSource == series.Source ? aggregateSource : ReportDataSource.Hybrid;
        }

        return new PlatformInstitutionRanking(
            new ReportPeriod(from, to),
            aggregateSource ?? ReportDataSource.Live,
            items.OrderByDescending(i => i.Total).ToList());
    }

    public async Task<PlatformStudentsRanking> GetPlatformStudentsByInstitutionAsync(
        DateOnly asOf,
        CancellationToken cancellationToken = default)
    {
        var asOfEnd = asOf.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var rows = await db.Institutions.AsNoTracking()
            .Where(i => i.IsActive)
            .Select(i => new PlatformStudentsItem(
                i.Id,
                i.DisplayName,
                i.Students.Count(s => s.IsActive && s.CreatedAt < asOfEnd)))
            .OrderByDescending(i => i.TotalStudents)
            .ToListAsync(cancellationToken);

        return new PlatformStudentsRanking(asOf, rows);
    }

    private async Task<TimeSeriesReport> BuildTimeSeriesAsync(
        Guid institutionId,
        DateOnly from,
        DateOnly to,
        Func<InstitutionMetricsDaily, int> snapshotSelector,
        Func<Guid, DateOnly, CancellationToken, Task<int>> liveCounter,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var snapshots = await db.InstitutionMetricsDailies.AsNoTracking()
            .Where(m => m.InstitutionId == institutionId && m.MetricDate >= from && m.MetricDate <= to)
            .ToDictionaryAsync(m => m.MetricDate, cancellationToken);

        var series = new List<TimeSeriesPoint>();
        var snapshotDays = 0;
        var liveDays = 0;

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var useLive = !snapshots.TryGetValue(date, out var row) || date == today;
            int value;
            if (useLive)
            {
                value = await liveCounter(institutionId, date, cancellationToken);
                liveDays++;
            }
            else
            {
                value = snapshotSelector(row!);
                snapshotDays++;
            }

            series.Add(new TimeSeriesPoint(date, value));
        }

        var source = ResolveSource(snapshotDays, liveDays, series.Count);
        return new TimeSeriesReport(new ReportPeriod(from, to), source, series.Sum(s => s.Value), series);
    }

    private async Task<VerificationOutcomesReport> BuildVerificationOutcomesAsync(
        Guid institutionId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var snapshots = await db.InstitutionMetricsDailies.AsNoTracking()
            .Where(m => m.InstitutionId == institutionId && m.MetricDate >= from && m.MetricDate <= to)
            .ToDictionaryAsync(m => m.MetricDate, cancellationToken);

        var series = new List<VerificationOutcomePoint>();
        var snapshotDays = 0;
        var liveDays = 0;

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            int valid;
            int invalid;
            var useLive = !snapshots.TryGetValue(date, out var row) || date == today;
            if (useLive)
            {
                (valid, invalid) = await CountVerificationOutcomesLiveAsync(institutionId, date, cancellationToken);
                liveDays++;
            }
            else
            {
                valid = row!.VerificationsValid;
                invalid = row.VerificationsInvalid;
                snapshotDays++;
            }

            series.Add(new VerificationOutcomePoint(date, valid, invalid));
        }

        var source = ResolveSource(snapshotDays, liveDays, series.Count);
        return new VerificationOutcomesReport(
            new ReportPeriod(from, to),
            source,
            series.Sum(s => s.Valid),
            series.Sum(s => s.Invalid),
            series);
    }

    private static ReportDataSource ResolveSource(int snapshotDays, int liveDays, int totalDays)
    {
        if (snapshotDays == totalDays)
        {
            return ReportDataSource.Snapshot;
        }

        if (liveDays == totalDays)
        {
            return ReportDataSource.Live;
        }

        return ReportDataSource.Hybrid;
    }

    private Task<int> CountCredentialsIssuedLiveAsync(
        Guid institutionId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var (start, end) = DayBounds(date);
        return db.Credentials.AsNoTracking()
            .CountAsync(
                c => c.InstitutionId == institutionId
                     && c.IssuedAt >= start
                     && c.IssuedAt < end,
                cancellationToken);
    }

    private Task<int> CountCredentialsRevokedLiveAsync(
        Guid institutionId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var (start, end) = DayBounds(date);
        return db.Credentials.AsNoTracking()
            .CountAsync(
                c => c.InstitutionId == institutionId
                     && c.RevokedAt != null
                     && c.RevokedAt >= start
                     && c.RevokedAt < end,
                cancellationToken);
    }

    private Task<int> CountVerificationsLiveAsync(
        Guid institutionId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var (start, end) = DayBounds(date);
        return db.VerificationLogs.AsNoTracking()
            .Where(vl => vl.CredentialId != null
                         && vl.VerifiedAt >= start
                         && vl.VerifiedAt < end)
            .Join(
                db.Credentials.AsNoTracking(),
                vl => vl.CredentialId,
                c => c.Id,
                (_, c) => c)
            .CountAsync(c => c.InstitutionId == institutionId, cancellationToken);
    }

    private async Task<(int Valid, int Invalid)> CountVerificationOutcomesLiveAsync(
        Guid institutionId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var (start, end) = DayBounds(date);
        var rows = await db.VerificationLogs.AsNoTracking()
            .Where(vl => vl.CredentialId != null
                         && vl.VerifiedAt >= start
                         && vl.VerifiedAt < end)
            .Join(
                db.Credentials.AsNoTracking(),
                vl => vl.CredentialId,
                c => c.Id,
                (vl, c) => new { c.InstitutionId, vl.Result })
            .Where(x => x.InstitutionId == institutionId)
            .GroupBy(x => x.Result == VerificationResult.Valid)
            .Select(g => new { IsValid = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var valid = rows.FirstOrDefault(r => r.IsValid)?.Count ?? 0;
        var invalid = rows.FirstOrDefault(r => !r.IsValid)?.Count ?? 0;
        return (valid, invalid);
    }

    private static (DateTime Start, DateTime End) DayBounds(DateOnly date)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return (start, start.AddDays(1));
    }
}
