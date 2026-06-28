namespace Academy.Application;

public sealed record AcademyFailure(
    string ErrorCode,
    int StatusCode,
    string Detail);

public abstract record AcademyResult<T>;

public sealed record AcademySuccess<T>(T Value) : AcademyResult<T>;

public sealed record AcademyFailureResult<T>(AcademyFailure Failure) : AcademyResult<T>;

public sealed record CreateInstitutionCommand(
    string Code,
    string LegalName,
    string DisplayName,
    string ContactEmail,
    string CountryCode = "CL",
    string? WebsiteUrl = null);

public sealed record InstitutionSummary(
    Guid Id,
    string Code,
    string LegalName,
    string DisplayName,
    string? Did,
    string? IssuerWalletAddress,
    string CountryCode,
    string? WebsiteUrl,
    bool IsActive,
    DateTimeOffset RegisteredAt);

public sealed record InstitutionCreated(
    InstitutionSummary Institution,
    InstitutionInvitationCreated Invitation);

public sealed record CreateCareerCommand(
    Guid InstitutionId,
    string Code,
    string Name);

public sealed record CareerSummary(
    Guid Id,
    Guid InstitutionId,
    string Code,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record CreateStudentCommand(
    Guid InstitutionId,
    string? ExternalReference,
    int? EnrollmentYear,
    string? WalletAddress);

public sealed record StudentSummary(
    Guid Id,
    Guid InstitutionId,
    string? ExternalReference,
    int? EnrollmentYear,
    Guid? PrimaryWalletId,
    string? PrimaryWalletAddress,
    string? PrimaryWalletDid,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record CreateInstitutionInvitationCommand(
    Guid InstitutionId,
    string Email,
    string Role = InstitutionRoles.Issuer,
    Guid? CreatedByUserId = null);

public sealed record InstitutionInvitationCreated(
    Guid Id,
    Guid InstitutionId,
    string Email,
    string Role,
    string InvitationUrl,
    DateTimeOffset ExpiresAt);

public sealed record AcceptInstitutionInvitationCommand(
    string Token,
    string WalletAddress,
    string? DisplayName);

public sealed record InstitutionInvitationAccepted(
    Guid InstitutionId,
    Guid UserId,
    string WalletAddress,
    string Did,
    string Role);

public static class InstitutionRoles
{
    public const string Admin = "admin";
    public const string Issuer = "issuer";
    public const string Student = "student";

    public static bool IsValid(string role) =>
        string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, Issuer, StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, Student, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string role) => role.Trim().ToLowerInvariant();
}
