namespace Reports.Api.Models;

public sealed record TimeSeriesReportResponse(
    ReportPeriodResponse Period,
    string Source,
    int Total,
    IReadOnlyList<TimeSeriesPointResponse> Series);

public sealed record ReportPeriodResponse(string From, string To);

public sealed record TimeSeriesPointResponse(string Date, int Value);

public sealed record VerificationOutcomesReportResponse(
    ReportPeriodResponse Period,
    string Source,
    int ValidTotal,
    int InvalidTotal,
    IReadOnlyList<VerificationOutcomePointResponse> Series);

public sealed record VerificationOutcomePointResponse(string Date, int Valid, int Invalid);

public sealed record CredentialsPerStudentReportResponse(
    string AsOf,
    int TotalStudents,
    int TotalCredentials,
    double AverageCredentialsPerStudent);

public sealed record PlatformCredentialsByInstitutionResponse(
    ReportPeriodResponse Period,
    string Source,
    IReadOnlyList<PlatformRankingItemResponse> Items);

public sealed record PlatformRankingItemResponse(
    Guid InstitutionId,
    string DisplayName,
    int Total);

public sealed record PlatformStudentsByInstitutionResponse(
    string AsOf,
    IReadOnlyList<PlatformStudentsItemResponse> Items);

public sealed record PlatformStudentsItemResponse(
    Guid InstitutionId,
    string DisplayName,
    int TotalStudents);
