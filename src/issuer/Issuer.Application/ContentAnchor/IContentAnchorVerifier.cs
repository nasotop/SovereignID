namespace Issuer.Application.ContentAnchor;

public sealed record ContentAnchorCheck(
    string IpfsCid,
    string IpfsGatewayUrl,
    string ContentHash);

public interface IContentAnchorVerifier
{
    Task<ContentAnchorVerificationResult> VerifyAsync(
        ContentAnchorCheck check,
        CancellationToken cancellationToken);
}

public sealed record ContentAnchorVerificationResult(bool IsValid, string? ErrorCode, string? Detail)
{
    public static ContentAnchorVerificationResult Success() => new(true, null, null);

    public static ContentAnchorVerificationResult Failure(string errorCode, string detail) =>
        new(false, errorCode, detail);
}
