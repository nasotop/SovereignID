using Reports.Domain;

namespace Reports.Application;

public sealed class ReportCatalog(IReportCatalogReader reader) : IReportCatalog
{
    private const int MaxPeriodDays = 366;

    public async Task<TimeSeriesReport> GetInstitutionTimeSeriesAsync(
        InstitutionReportId reportId,
        Guid institutionId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(from, to);
        await EnsureInstitutionExistsAsync(institutionId, cancellationToken);
        return await reader.GetInstitutionTimeSeriesAsync(reportId, institutionId, from, to, cancellationToken);
    }

    public async Task<CredentialsPerStudentReport> GetInstitutionCredentialsPerStudentAsync(
        Guid institutionId,
        DateOnly asOf,
        CancellationToken cancellationToken = default)
    {
        await EnsureInstitutionExistsAsync(institutionId, cancellationToken);
        return await reader.GetInstitutionCredentialsPerStudentAsync(institutionId, asOf, cancellationToken);
    }

    public async Task<VerificationOutcomesReport> GetInstitutionVerificationOutcomesAsync(
        Guid institutionId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(from, to);
        await EnsureInstitutionExistsAsync(institutionId, cancellationToken);
        return await reader.GetInstitutionVerificationOutcomesAsync(institutionId, from, to, cancellationToken);
    }

    public async Task<PlatformInstitutionRanking> GetPlatformCredentialsByInstitutionAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(from, to);
        return await reader.GetPlatformCredentialsByInstitutionAsync(from, to, cancellationToken);
    }

    public Task<PlatformStudentsRanking> GetPlatformStudentsByInstitutionAsync(
        DateOnly asOf,
        CancellationToken cancellationToken = default) =>
        reader.GetPlatformStudentsByInstitutionAsync(asOf, cancellationToken);

    private static void ValidatePeriod(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            throw new ReportsFailureException(new ReportsFailure(
                ReportsErrorCodes.InvalidReportPeriod,
                400,
                "The report period end date must be on or after the start date."));
        }

        if (to.DayNumber - from.DayNumber > MaxPeriodDays)
        {
            throw new ReportsFailureException(new ReportsFailure(
                ReportsErrorCodes.InvalidReportPeriod,
                400,
                $"Report periods cannot exceed {MaxPeriodDays} days."));
        }
    }

    private async Task EnsureInstitutionExistsAsync(Guid institutionId, CancellationToken cancellationToken)
    {
        if (!await reader.InstitutionExistsAsync(institutionId, cancellationToken))
        {
            throw new ReportsFailureException(new ReportsFailure(
                ReportsErrorCodes.InstitutionNotFound,
                404,
                $"Institution '{institutionId}' was not found."));
        }
    }
}

public sealed class ReportsFailureException(ReportsFailure failure) : Exception(failure.Detail)
{
    public ReportsFailure Failure { get; } = failure;
}
