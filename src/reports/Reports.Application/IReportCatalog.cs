using Reports.Domain;

namespace Reports.Application;

public interface IReportCatalog
{
    Task<TimeSeriesReport> GetInstitutionTimeSeriesAsync(
        InstitutionReportId reportId,
        Guid institutionId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<CredentialsPerStudentReport> GetInstitutionCredentialsPerStudentAsync(
        Guid institutionId,
        DateOnly asOf,
        CancellationToken cancellationToken = default);

    Task<VerificationOutcomesReport> GetInstitutionVerificationOutcomesAsync(
        Guid institutionId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<PlatformInstitutionRanking> GetPlatformCredentialsByInstitutionAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<PlatformStudentsRanking> GetPlatformStudentsByInstitutionAsync(
        DateOnly asOf,
        CancellationToken cancellationToken = default);
}
