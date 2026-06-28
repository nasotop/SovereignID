namespace Academy.Infrastructure.Persistence.Entities;

internal enum UserRole
{
    admin,
    issuer,
    student
}

internal enum WalletStatus
{
    active,
    rotated,
    revoked
}

internal sealed class InstitutionEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Did { get; set; }
    public string? IssuerWalletAddress { get; set; }
    public string? PublicKey { get; set; }
    public string CountryCode { get; set; } = "CL";
    public string? WebsiteUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime RegisteredAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
}

internal sealed class InstitutionInvitationEntity
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string InvitationUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public Guid? AcceptedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? RevokedAt { get; set; }
}

internal sealed class UserEntity
{
    public Guid Id { get; set; }
    public string WalletAddress { get; set; } = string.Empty;
    public string Did { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

internal sealed class InstitutionUserEntity
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid UserId { get; set; }
    public UserRole Role { get; set; }
    public Guid? GrantedByUserId { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}

internal sealed class StudentEntity
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public string? ExternalReference { get; set; }
    public int? EnrollmentYear { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

internal sealed class StudentWalletEntity
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string WalletAddress { get; set; } = string.Empty;
    public string Did { get; set; } = string.Empty;
    public WalletStatus Status { get; set; } = WalletStatus.active;
    public bool IsPrimary { get; set; } = true;
    public DateTime ActivatedAt { get; set; }
    public DateTime? RotatedAt { get; set; }
    public string? RotationReason { get; set; }
}

internal sealed class CareerEntity
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
