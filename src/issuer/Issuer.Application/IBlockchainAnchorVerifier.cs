namespace Issuer.Application;

public sealed record BlockchainAnchorProof(
    string TransactionHash,
    long BlockNumber,
    int ChainId,
    string ContentHash,
    string Eip712Signature,
    Guid? CredentialId = null);

public interface IBlockchainAnchorVerifier
{
    Task<BlockchainAnchorVerificationResult> VerifyIssueAnchorAsync(
        BlockchainAnchorProof proof,
        string expectedIssuerWalletAddress,
        CancellationToken cancellationToken);

    Task<BlockchainAnchorVerificationResult> VerifyRevocationAnchorAsync(
        BlockchainAnchorProof proof,
        string expectedIssuerWalletAddress,
        Guid credentialId,
        CancellationToken cancellationToken);
}

public sealed record BlockchainAnchorVerificationResult(bool IsValid, string? ErrorCode, string? Detail)
{
    public static BlockchainAnchorVerificationResult Success() => new(true, null, null);

    public static BlockchainAnchorVerificationResult Failure(string errorCode, string detail) =>
        new(false, errorCode, detail);
}
