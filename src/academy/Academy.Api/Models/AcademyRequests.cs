namespace Academy.Api.Models;

public sealed record CreateInstitutionRequest(
    string Code,
    string LegalName,
    string DisplayName,
    string ContactEmail,
    string CountryCode = "CL",
    string? WebsiteUrl = null);

public sealed record UpdateInstitutionRequest(
    string LegalName,
    string DisplayName,
    string CountryCode = "CL",
    string? WebsiteUrl = null,
    bool IsActive = true);

public sealed record CreateCareerRequest(
    string Code,
    string Name);

public sealed record UpdateCareerRequest(
    string Code,
    string Name);

public sealed record CreateStudentRequest(
    string? ExternalReference,
    int? EnrollmentYear,
    string? WalletAddress);

public sealed record AddStudentWalletRequest(
    string WalletAddress,
    bool MakePrimary = true);

public sealed record CreateInstitutionInvitationRequest(
    string Email,
    string Role = "issuer",
    Guid? CreatedByUserId = null);

public sealed record UpdateInstitutionUserRoleRequest(
    string Role);

public sealed record AcceptInstitutionInvitationRequest(
    string Token,
    string WalletAddress,
    string? DisplayName);

public sealed record UpdateHolderProfileRequest(
    string? DisplayName,
    string? FullName,
    DateOnly? BirthDate,
    string? ContactEmail,
    string? CountryCode,
    string? PhoneNumber);
