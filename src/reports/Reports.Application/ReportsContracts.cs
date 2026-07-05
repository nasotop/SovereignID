namespace Reports.Application;

public sealed record ReportsFailure(
    string ErrorCode,
    int StatusCode,
    string Detail);

public static class ReportsErrorCodes
{
    public const string InvalidReportPeriod = "invalid_report_period";
    public const string InstitutionNotFound = "institution_not_found";
}
