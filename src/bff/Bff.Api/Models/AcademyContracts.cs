namespace Bff.Api.Models;

public sealed record CreateStudentRequest(
    string? ExternalReference,
    int? EnrollmentYear,
    string? WalletAddress);

public sealed record UpdateCareerRequest(
    string Code,
    string Name);

public sealed record AddStudentWalletRequest(
    string WalletAddress,
    bool MakePrimary = true);

public sealed record UpdateInstitutionUserRoleRequest(
    string Role);

public sealed record UpdateHolderProfileRequest(
    string? DisplayName,
    string? FullName,
    DateOnly? BirthDate,
    string? ContactEmail,
    string? CountryCode,
    string? PhoneNumber);

public sealed record StudentWalletSummary(
    Guid Id,
    Guid StudentId,
    string WalletAddress,
    string Did,
    string Status,
    bool IsPrimary,
    DateTimeOffset ActivatedAt);

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
