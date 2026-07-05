namespace Reports.Domain;

public enum InstitutionReportId
{
    CredentialsIssued,
    CredentialReads,
    CredentialsPerStudent,
    CredentialsRevoked,
    VerificationOutcomes
}

public enum PlatformReportId
{
    CredentialsByInstitution,
    StudentsByInstitution
}

public enum ReportDataSource
{
    Snapshot,
    Live,
    Hybrid
}

public sealed record ReportPeriod(DateOnly From, DateOnly To);

public sealed record TimeSeriesPoint(DateOnly Date, int Value);

public sealed record TimeSeriesReport(
    ReportPeriod Period,
    ReportDataSource Source,
    int Total,
    IReadOnlyList<TimeSeriesPoint> Series);

public sealed record VerificationOutcomePoint(DateOnly Date, int Valid, int Invalid);

public sealed record VerificationOutcomesReport(
    ReportPeriod Period,
    ReportDataSource Source,
    int ValidTotal,
    int InvalidTotal,
    IReadOnlyList<VerificationOutcomePoint> Series);

public sealed record CredentialsPerStudentReport(
    DateOnly AsOf,
    int TotalStudents,
    int TotalCredentials,
    double AverageCredentialsPerStudent);

public sealed record PlatformRankingItem(
    Guid InstitutionId,
    string DisplayName,
    int Total);

public sealed record PlatformInstitutionRanking(
    ReportPeriod Period,
    ReportDataSource Source,
    IReadOnlyList<PlatformRankingItem> Items);

public sealed record PlatformStudentsItem(
    Guid InstitutionId,
    string DisplayName,
    int TotalStudents);

public sealed record PlatformStudentsRanking(
    DateOnly AsOf,
    IReadOnlyList<PlatformStudentsItem> Items);
