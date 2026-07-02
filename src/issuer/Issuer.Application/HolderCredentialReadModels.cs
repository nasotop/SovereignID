namespace Issuer.Application;

public sealed record HolderCredentialSummary(
    Guid Id,
    string Title,
    string IssuerName,
    string TypeCode,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset? ExpiresAt);

public sealed record HolderCredentialIssuer(
    string Did,
    string DisplayName,
    string Code);

public sealed record HolderCredentialAnchors(
    string IpfsCid,
    string IpfsGatewayUrl,
    string ContentHash,
    string TransactionHash,
    long BlockNumber,
    int ChainId,
    string Eip712Signature);

public sealed record HolderCredentialDetail(
    Guid Id,
    string Title,
    string IssuerName,
    string TypeCode,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt,
    string SubjectDid,
    HolderCredentialIssuer Issuer,
    HolderCredentialAnchors Anchors,
    string? Metadata);
