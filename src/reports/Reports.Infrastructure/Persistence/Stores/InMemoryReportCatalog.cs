using Reports.Application;
using Reports.Domain;
using Reports.Infrastructure.Persistence.Generated.Entities;

namespace Reports.Infrastructure.Persistence.Stores;

internal sealed class InMemoryReportData
{
    public List<Institution> Institutions { get; } = [];
    public List<Student> Students { get; } = [];
    public List<Credential> Credentials { get; } = [];
    public List<VerificationLog> VerificationLogs { get; } = [];
    public List<InstitutionMetricsDaily> Metrics { get; } = [];
}

internal sealed class InMemoryReportCatalog(InMemoryReportData data, TimeProvider timeProvider) : IReportCatalogReader
{
    public static readonly Guid DemoInstitutionId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Task<bool> InstitutionExistsAsync(Guid institutionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(data.Institutions.Any(i => i.Id == institutionId));

    public Task<TimeSeriesReport> GetInstitutionTimeSeriesAsync(
        InstitutionReportId reportId,
        Guid institutionId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default) =>
        reportId switch
        {
            InstitutionReportId.CredentialsIssued => BuildTimeSeriesAsync(
                institutionId, from, to, m => m.CredentialsIssued, CountIssuedLive),
            InstitutionReportId.CredentialReads => BuildTimeSeriesAsync(
                institutionId, from, to, m => m.VerificationsTotal, CountReadsLive),
            InstitutionReportId.CredentialsRevoked => BuildTimeSeriesAsync(
                institutionId, from, to, m => m.CredentialsRevoked, CountRevokedLive),
            _ => throw new ArgumentOutOfRangeException(nameof(reportId), reportId, "Not a time-series institution report.")
        };

    public Task<CredentialsPerStudentReport> GetInstitutionCredentialsPerStudentAsync(
        Guid institutionId,
        DateOnly asOf,
        CancellationToken cancellationToken = default)
    {
        var asOfEnd = asOf.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var totalStudents = data.Students.Count(
            s => s.InstitutionId == institutionId && s.IsActive && s.CreatedAt < asOfEnd);
        var totalCredentials = data.Credentials.Count(
            c => c.InstitutionId == institutionId && c.IssuedAt < asOfEnd);
        var average = totalStudents == 0 ? 0d : Math.Round((double)totalCredentials / totalStudents, 1);
        return Task.FromResult(new CredentialsPerStudentReport(asOf, totalStudents, totalCredentials, average));
    }

    public Task<VerificationOutcomesReport> GetInstitutionVerificationOutcomesAsync(
        Guid institutionId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var snapshots = data.Metrics
            .Where(m => m.InstitutionId == institutionId && m.MetricDate >= from && m.MetricDate <= to)
            .ToDictionary(m => m.MetricDate);

        var series = new List<VerificationOutcomePoint>();
        var snapshotDays = 0;
        var liveDays = 0;

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            int valid;
            int invalid;
            if (!snapshots.TryGetValue(date, out var row) || date == today)
            {
                (valid, invalid) = CountOutcomesLive(institutionId, date);
                liveDays++;
            }
            else
            {
                valid = row.VerificationsValid;
                invalid = row.VerificationsInvalid;
                snapshotDays++;
            }

            series.Add(new VerificationOutcomePoint(date, valid, invalid));
        }

        var source = ResolveSource(snapshotDays, liveDays, series.Count);
        return Task.FromResult(new VerificationOutcomesReport(
            new ReportPeriod(from, to),
            source,
            series.Sum(s => s.Valid),
            series.Sum(s => s.Invalid),
            series));
    }

    public async Task<PlatformInstitutionRanking> GetPlatformCredentialsByInstitutionAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var items = new List<PlatformRankingItem>();
        ReportDataSource? aggregateSource = null;

        foreach (var institution in data.Institutions.Where(i => i.IsActive))
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

    public Task<PlatformStudentsRanking> GetPlatformStudentsByInstitutionAsync(
        DateOnly asOf,
        CancellationToken cancellationToken = default)
    {
        var asOfEnd = asOf.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var items = data.Institutions
            .Where(i => i.IsActive)
            .Select(i => new PlatformStudentsItem(
                i.Id,
                i.DisplayName,
                data.Students.Count(s => s.InstitutionId == i.Id && s.IsActive && s.CreatedAt < asOfEnd)))
            .OrderByDescending(i => i.TotalStudents)
            .ToList();

        return Task.FromResult(new PlatformStudentsRanking(asOf, items));
    }

    private Task<TimeSeriesReport> BuildTimeSeriesAsync(
        Guid institutionId,
        DateOnly from,
        DateOnly to,
        Func<InstitutionMetricsDaily, int> snapshotSelector,
        Func<Guid, DateOnly, int> liveCounter)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var snapshots = data.Metrics
            .Where(m => m.InstitutionId == institutionId && m.MetricDate >= from && m.MetricDate <= to)
            .ToDictionary(m => m.MetricDate);

        var series = new List<TimeSeriesPoint>();
        var snapshotDays = 0;
        var liveDays = 0;

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            int value;
            if (!snapshots.TryGetValue(date, out var row) || date == today)
            {
                value = liveCounter(institutionId, date);
                liveDays++;
            }
            else
            {
                value = snapshotSelector(row);
                snapshotDays++;
            }

            series.Add(new TimeSeriesPoint(date, value));
        }

        var source = ResolveSource(snapshotDays, liveDays, series.Count);
        return Task.FromResult(new TimeSeriesReport(
            new ReportPeriod(from, to),
            source,
            series.Sum(s => s.Value),
            series));
    }

    private int CountIssuedLive(Guid institutionId, DateOnly date)
    {
        var (start, end) = DayBounds(date);
        return data.Credentials.Count(
            c => c.InstitutionId == institutionId && c.IssuedAt >= start && c.IssuedAt < end);
    }

    private int CountRevokedLive(Guid institutionId, DateOnly date)
    {
        var (start, end) = DayBounds(date);
        return data.Credentials.Count(
            c => c.InstitutionId == institutionId
                 && c.RevokedAt != null
                 && c.RevokedAt >= start
                 && c.RevokedAt < end);
    }

    private int CountReadsLive(Guid institutionId, DateOnly date)
    {
        var (start, end) = DayBounds(date);
        return data.VerificationLogs.Count(vl =>
            vl.CredentialId != null
            && vl.VerifiedAt >= start
            && vl.VerifiedAt < end
            && data.Credentials.Any(c => c.Id == vl.CredentialId && c.InstitutionId == institutionId));
    }

    private (int Valid, int Invalid) CountOutcomesLive(Guid institutionId, DateOnly date)
    {
        var (start, end) = DayBounds(date);
        var logs = data.VerificationLogs.Where(vl =>
            vl.CredentialId != null
            && vl.VerifiedAt >= start
            && vl.VerifiedAt < end
            && data.Credentials.Any(c => c.Id == vl.CredentialId && c.InstitutionId == institutionId));

        var valid = logs.Count(vl => vl.Result == VerificationResult.Valid);
        var invalid = logs.Count(vl => vl.Result != VerificationResult.Valid);
        return (valid, invalid);
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

    private static (DateTime Start, DateTime End) DayBounds(DateOnly date)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return (start, start.AddDays(1));
    }
}

internal static class InMemoryReportDataSeed
{
    public static InMemoryReportData CreateDefault(TimeProvider timeProvider)
    {
        var data = new InMemoryReportData();
        var institution = new Institution
        {
            Id = InMemoryReportCatalog.DemoInstitutionId,
            Code = "DUOC",
            LegalName = "Duoc UC",
            DisplayName = "Duoc UC",
            IsActive = true
        };
        data.Institutions.Add(institution);

        var student = new Student
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            InstitutionId = institution.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        };
        data.Students.Add(student);

        var issuedAt = timeProvider.GetUtcNow().UtcDateTime.AddDays(-2);
        data.Credentials.Add(new Credential
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            InstitutionId = institution.Id,
            StudentId = student.Id,
            Status = CredentialStatus.Active,
            IssuedAt = issuedAt
        });

        data.VerificationLogs.Add(new VerificationLog
        {
            Id = Guid.NewGuid(),
            CredentialId = data.Credentials[0].Id,
            Result = VerificationResult.Valid,
            VerifiedAt = issuedAt.AddHours(1),
            VerifierIp = System.Net.IPAddress.Parse("127.0.0.1")
        });

        return data;
    }
}
