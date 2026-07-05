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

public sealed record AddStudentWalletCommand(
    Guid InstitutionId,
    Guid StudentId,
    string WalletAddress,
    bool MakePrimary = true);

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

public sealed record StudentWalletSummary(
    Guid Id,
    Guid StudentId,
    string WalletAddress,
    string Did,
    string Status,
    bool IsPrimary,
    DateTimeOffset ActivatedAt);

public sealed record HolderProfile(
    string WalletAddress,
    string Did,
    string? DisplayName,
    string? FullName,
    DateOnly? BirthDate,
    string? ContactEmail,
    string? CountryCode,
    string? PhoneNumber,
    DateTimeOffset? UpdatedAt);

public sealed record UpdateHolderProfileCommand(
    string WalletAddress,
    string Did,
    string? DisplayName,
    string? FullName,
    DateOnly? BirthDate,
    string? ContactEmail,
    string? CountryCode,
    string? PhoneNumber);

public sealed record HolderInstitutionSummary(
    Guid InstitutionId,
    string InstitutionCode,
    string InstitutionName,
    Guid StudentId,
    string? ExternalReference,
    int? EnrollmentYear,
    string WalletAddress,
    string Did,
    bool IsPrimary,
    DateTimeOffset LinkedAt);

public sealed record HolderDashboard(
    HolderProfile Profile,
    IReadOnlyList<HolderInstitutionSummary> Institutions);

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

public sealed record InstitutionUserSummary(
    Guid Id,
    Guid InstitutionId,
    Guid UserId,
    string WalletAddress,
    string Did,
    string? Email,
    string? DisplayName,
    string Role,
    DateTimeOffset GrantedAt,
    DateTimeOffset? RevokedAt);

public static class InstitutionRoles
{
    public const string Admin = "admin";
    public const string Issuer = "issuer";
    public const string Student = "student";
    public const string Viewer = "viewer";

    public static bool IsValid(string role) =>
        string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, Issuer, StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, Student, StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, Viewer, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string role) => role.Trim().ToLowerInvariant();
}
