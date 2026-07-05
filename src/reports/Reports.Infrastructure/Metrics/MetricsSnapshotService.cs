using Microsoft.EntityFrameworkCore;
using Reports.Infrastructure.Persistence.Generated;
using Reports.Infrastructure.Persistence.Generated.Entities;
using Reports.Infrastructure.Persistence.Stores;

namespace Reports.Infrastructure.Metrics;

public interface IMetricsSnapshotService
{
    Task UpsertDailyMetricsAsync(DateOnly metricDate, CancellationToken cancellationToken = default);

    Task UpsertRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}

internal sealed class PostgresMetricsSnapshotService(ReportsDbContext db, TimeProvider timeProvider) : IMetricsSnapshotService
{
    public async Task UpsertRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            await UpsertDailyMetricsAsync(date, cancellationToken);
        }
    }

    public async Task UpsertDailyMetricsAsync(DateOnly metricDate, CancellationToken cancellationToken = default)
    {
        var (start, end) = DayBounds(metricDate);
        var institutionIds = await db.Institutions.AsNoTracking()
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        var computedAt = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var institutionId in institutionIds)
        {
            var credentialsIssued = await db.Credentials.CountAsync(
                c => c.InstitutionId == institutionId && c.IssuedAt >= start && c.IssuedAt < end,
                cancellationToken);

            var credentialsRevoked = await db.Credentials.CountAsync(
                c => c.InstitutionId == institutionId
                     && c.RevokedAt != null
                     && c.RevokedAt >= start
                     && c.RevokedAt < end,
                cancellationToken);

            var verificationRows = await db.VerificationLogs.AsNoTracking()
                .Where(vl => vl.CredentialId != null && vl.VerifiedAt >= start && vl.VerifiedAt < end)
                .Join(
                    db.Credentials.AsNoTracking(),
                    vl => vl.CredentialId,
                    c => c.Id,
                    (vl, c) => new { c.InstitutionId, c.StudentId, vl.Result })
                .Where(x => x.InstitutionId == institutionId)
                .ToListAsync(cancellationToken);

            var verificationsTotal = verificationRows.Count;
            var verificationsValid = verificationRows.Count(x => x.Result == VerificationResult.Valid);
            var verificationsInvalid = verificationRows.Count(
                x => x.Result != VerificationResult.Valid);

            var issuedStudentIds = await db.Credentials.AsNoTracking()
                .Where(c => c.InstitutionId == institutionId && c.IssuedAt >= start && c.IssuedAt < end)
                .Select(c => c.StudentId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var verifiedStudentIds = verificationRows
                .Select(x => x.StudentId)
                .Distinct();

            var uniqueStudentsActive = issuedStudentIds
                .Concat(verifiedStudentIds)
                .Distinct()
                .Count();

            var existing = await db.InstitutionMetricsDailies
                .FirstOrDefaultAsync(
                    m => m.InstitutionId == institutionId && m.MetricDate == metricDate,
                    cancellationToken);

            if (existing is null)
            {
                db.InstitutionMetricsDailies.Add(new InstitutionMetricsDaily
                {
                    InstitutionId = institutionId,
                    MetricDate = metricDate,
                    CredentialsIssued = credentialsIssued,
                    CredentialsRevoked = credentialsRevoked,
                    VerificationsTotal = verificationsTotal,
                    VerificationsValid = verificationsValid,
                    VerificationsInvalid = verificationsInvalid,
                    UniqueStudentsActive = uniqueStudentsActive,
                    ComputedAt = computedAt
                });
            }
            else
            {
                existing.CredentialsIssued = credentialsIssued;
                existing.CredentialsRevoked = credentialsRevoked;
                existing.VerificationsTotal = verificationsTotal;
                existing.VerificationsValid = verificationsValid;
                existing.VerificationsInvalid = verificationsInvalid;
                existing.UniqueStudentsActive = uniqueStudentsActive;
                existing.ComputedAt = computedAt;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static (DateTime Start, DateTime End) DayBounds(DateOnly date)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return (start, start.AddDays(1));
    }
}

internal sealed class InMemoryMetricsSnapshotService(InMemoryReportData data, TimeProvider timeProvider) : IMetricsSnapshotService
{
    public Task UpsertRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            UpsertDaily(date);
        }

        return Task.CompletedTask;
    }

    public Task UpsertDailyMetricsAsync(DateOnly metricDate, CancellationToken cancellationToken = default)
    {
        UpsertDaily(metricDate);
        return Task.CompletedTask;
    }

    private void UpsertDaily(DateOnly metricDate)
    {
        var (start, end) = DayBounds(metricDate);
        var computedAt = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var institution in data.Institutions)
        {
            var credentialsIssued = data.Credentials.Count(
                c => c.InstitutionId == institution.Id && c.IssuedAt >= start && c.IssuedAt < end);
            var credentialsRevoked = data.Credentials.Count(
                c => c.InstitutionId == institution.Id
                     && c.RevokedAt != null
                     && c.RevokedAt >= start
                     && c.RevokedAt < end);

            var verificationRows = data.VerificationLogs
                .Where(vl => vl.CredentialId != null && vl.VerifiedAt >= start && vl.VerifiedAt < end)
                .Select(vl => new
                {
                    Credential = data.Credentials.First(c => c.Id == vl.CredentialId),
                    vl.Result
                })
                .Where(x => x.Credential.InstitutionId == institution.Id)
                .ToList();

            var verificationsTotal = verificationRows.Count;
            var verificationsValid = verificationRows.Count(x => x.Result == VerificationResult.Valid);
            var verificationsInvalid = verificationRows.Count(x => x.Result != VerificationResult.Valid);
            var uniqueStudentsActive = verificationRows
                .Select(x => x.Credential.StudentId)
                .Concat(data.Credentials
                    .Where(c => c.InstitutionId == institution.Id && c.IssuedAt >= start && c.IssuedAt < end)
                    .Select(c => c.StudentId))
                .Distinct()
                .Count();

            var existing = data.Metrics.FirstOrDefault(
                m => m.InstitutionId == institution.Id && m.MetricDate == metricDate);

            if (existing is null)
            {
                data.Metrics.Add(new InstitutionMetricsDaily
                {
                    Id = data.Metrics.Count + 1,
                    InstitutionId = institution.Id,
                    MetricDate = metricDate,
                    CredentialsIssued = credentialsIssued,
                    CredentialsRevoked = credentialsRevoked,
                    VerificationsTotal = verificationsTotal,
                    VerificationsValid = verificationsValid,
                    VerificationsInvalid = verificationsInvalid,
                    UniqueStudentsActive = uniqueStudentsActive,
                    ComputedAt = computedAt
                });
            }
            else
            {
                existing.CredentialsIssued = credentialsIssued;
                existing.CredentialsRevoked = credentialsRevoked;
                existing.VerificationsTotal = verificationsTotal;
                existing.VerificationsValid = verificationsValid;
                existing.VerificationsInvalid = verificationsInvalid;
                existing.UniqueStudentsActive = uniqueStudentsActive;
                existing.ComputedAt = computedAt;
            }
        }
    }

    private static (DateTime Start, DateTime End) DayBounds(DateOnly date)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return (start, start.AddDays(1));
    }
}
