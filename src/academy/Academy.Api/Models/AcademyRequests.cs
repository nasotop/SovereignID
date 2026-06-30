namespace Academy.Api.Models;

public sealed record CreateInstitutionRequest(
    string Code,
    string LegalName,
    string DisplayName,
    string ContactEmail,
    string CountryCode = "CL",
    string? WebsiteUrl = null);

public sealed record CreateCareerRequest(
    string Code,
    string Name);

public sealed record CreateStudentRequest(
    string? ExternalReference,
    int? EnrollmentYear,
    string? WalletAddress);

public sealed record CreateInstitutionInvitationRequest(
    string Email,
    string Role = "issuer",
    Guid? CreatedByUserId = null);

public sealed record AcceptInstitutionInvitationRequest(
    string Token,
    string WalletAddress,
    string? DisplayName);
