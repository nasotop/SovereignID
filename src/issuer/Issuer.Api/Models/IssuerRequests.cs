using System.Text.Json;

namespace Issuer.Api.Models;

public sealed record LinkInstitutionIssuerWalletRequest(
    string WalletAddress,
    string Did,
    string? PublicKey);

public sealed record AnchorCredentialDocumentRequest(JsonElement Document);

public sealed record ContentAnchorResponse(
    string ContentHash,
    string IpfsCid,
    string IpfsGatewayUrl);

public sealed record LinkStudentTitleRequest(
    Guid? CredentialId,
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

public sealed record RevokeCredentialRequest(
    string Reason,
    string RevocationTxHash,
    long BlockNumber,
    int? ChainId,
    string Eip712Signature,
    Guid? RevokedByUserId);
