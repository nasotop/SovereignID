using System.Text.Json;

namespace Bff.Api.Models;

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

public sealed record LinkInstitutionIssuerWalletRequest(
    string WalletAddress,
    string Did,
    string? PublicKey);

public sealed record InstitutionIssuerWalletLinked(
    Guid InstitutionId,
    string WalletAddress,
    string Did);

public sealed record LinkStudentTitleRequest(
    Guid? CareerId,
    string CredentialTypeCode,
    string IpfsCid,
    string IpfsGatewayUrl,
    string ContentHash,
    string TransactionHash,
    long BlockNumber,
    int? ChainId,
    string Eip712Signature,
    DateTimeOffset? ExpiresAt,
    JsonElement? Metadata);

public sealed record StudentTitleLinked(
    Guid CredentialId,
    Guid InstitutionId,
    Guid StudentId,
    Guid? CareerId,
    Guid IssuedToWalletId,
    string SubjectDid,
    string IssuerDid,
    string Status,
    DateTimeOffset IssuedAt);
