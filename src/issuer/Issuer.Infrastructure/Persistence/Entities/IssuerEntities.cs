namespace Issuer.Infrastructure.Persistence.Entities;

internal enum WalletStatus
{
    active,
    rotated,
    revoked
}

internal enum CredentialStatus
{
    active,
    revoked,
    expired
}

internal sealed class InstitutionEntity
{
    public Guid Id { get; set; }
    public string? Did { get; set; }
    public string? IssuerWalletAddress { get; set; }
    public string? PublicKey { get; set; }
    public bool IsActive { get; set; }
}

internal sealed class StudentEntity
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public string? ExternalReference { get; set; }
    public bool IsActive { get; set; }
}

internal sealed class StudentWalletEntity
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string Did { get; set; } = string.Empty;
    public WalletStatus Status { get; set; }
    public bool IsPrimary { get; set; }
}

internal sealed class CareerEntity
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public bool IsActive { get; set; }
}

internal sealed class CredentialTypeEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

internal sealed class CredentialEntity
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public int CredentialTypeId { get; set; }
    public Guid StudentId { get; set; }
    public Guid? CareerId { get; set; }
    public Guid IssuedToWalletId { get; set; }
    public string SubjectDid { get; set; } = string.Empty;
    public string IssuerDid { get; set; } = string.Empty;
    public string IpfsCid { get; set; } = string.Empty;
    public string IpfsGatewayUrl { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string TransactionHash { get; set; } = string.Empty;
    public long BlockNumber { get; set; }
    public int ChainId { get; set; }
    public string Eip712Signature { get; set; } = string.Empty;
    public CredentialStatus Status { get; set; } = CredentialStatus.active;
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? RevokedByUserId { get; set; }
    public string? RevocationReason { get; set; }
    public string? RevocationTxHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Metadata { get; set; }
}
