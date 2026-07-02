using System.Text.Json;

namespace Issuer.Application;

public sealed record IssuerFailure(
    string ErrorCode,
    int StatusCode,
    string Detail);

public abstract record IssuerResult<T>;

public sealed record IssuerSuccess<T>(T Value) : IssuerResult<T>;

public sealed record IssuerFailureResult<T>(IssuerFailure Failure) : IssuerResult<T>;

public sealed record LinkStudentTitleCommand(
    Guid StudentId,
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

public sealed record LinkInstitutionIssuerWalletCommand(
    Guid InstitutionId,
    string WalletAddress,
    string Did,
    string? PublicKey);

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

public sealed record InstitutionIssuerWalletLinked(
    Guid InstitutionId,
    string WalletAddress,
    string Did,
    string? PublicKey);
