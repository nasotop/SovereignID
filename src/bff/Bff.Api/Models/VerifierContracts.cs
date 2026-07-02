namespace Bff.Api.Models;

public sealed record VerificationRequest(string? CredentialId);

public sealed record VerificationChecksResponse(
    bool? Found,
    bool? NotRevoked,
    bool? NotExpired,
    bool? HashMatches,
    bool? OnChainExists,
    bool? SignatureValid);

public sealed record IssuerResponse(string Did, string DisplayName, string Code);

public sealed record AnchorsResponse(string IpfsCid, string ContentHash, string TransactionHash, int ChainId);

public sealed record CredentialResponse(
    Guid Id,
    string Type,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset? ExpiresAt,
    string SubjectDid,
    IssuerResponse Issuer,
    AnchorsResponse Anchors);

public sealed record VerificationResponse(
    string Result,
    VerificationChecksResponse Checks,
    CredentialResponse? Credential);
