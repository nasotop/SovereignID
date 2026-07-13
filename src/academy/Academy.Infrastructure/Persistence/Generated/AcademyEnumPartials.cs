namespace Academy.Infrastructure.Persistence.Generated.Entities;

internal enum UserRole
{
    admin,
    issuer,
    student,
    viewer
}

internal enum WalletStatus
{
    active,
    rotated,
    revoked
}

internal partial class InstitutionInvitation
{
    public UserRole Role { get; set; }
}

internal partial class InstitutionUser
{
    public UserRole Role { get; set; }
}

internal partial class StudentWallet
{
    public WalletStatus Status { get; set; } = WalletStatus.active;
}
