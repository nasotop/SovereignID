namespace Reports.Infrastructure.Persistence.Generated.Entities;

public enum VerificationResult
{
    Valid,
    InvalidSignature,
    Tampered,
    Revoked,
    Expired,
    NotFound,
    IpfsUnreachable
}

public enum CredentialStatus
{
    Active,
    Revoked,
    Expired
}
