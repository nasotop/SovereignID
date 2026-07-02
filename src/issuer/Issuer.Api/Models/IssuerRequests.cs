using System.Text.Json;

namespace Issuer.Api.Models;

public sealed record LinkInstitutionIssuerWalletRequest(
    string WalletAddress,
    string Did,
    string? PublicKey);

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
