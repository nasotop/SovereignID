using Issuer.Application;
using Microsoft.Extensions.Options;

namespace Issuer.Infrastructure.Blockchain;

internal sealed class NullBlockchainAnchorVerifier : IBlockchainAnchorVerifier
{
    public Task<BlockchainAnchorVerificationResult> VerifyIssueAnchorAsync(
        BlockchainAnchorProof proof,
        string expectedIssuerWalletAddress,
        CancellationToken cancellationToken) =>
        Task.FromResult(BlockchainAnchorVerificationResult.Success());

    public Task<BlockchainAnchorVerificationResult> VerifyRevocationAnchorAsync(
        BlockchainAnchorProof proof,
        string expectedIssuerWalletAddress,
        Guid credentialId,
        CancellationToken cancellationToken) =>
        Task.FromResult(BlockchainAnchorVerificationResult.Success());
}

internal sealed class ConfigurableBlockchainAnchorVerifier : IBlockchainAnchorVerifier
{
    private readonly IssuerOptions _options;
    private readonly RpcBlockchainAnchorVerifier _rpcVerifier;
    private readonly NullBlockchainAnchorVerifier _nullVerifier;

    public ConfigurableBlockchainAnchorVerifier(
        IOptions<IssuerOptions> options,
        RpcBlockchainAnchorVerifier rpcVerifier,
        NullBlockchainAnchorVerifier nullVerifier)
    {
        _options = options.Value;
        _rpcVerifier = rpcVerifier;
        _nullVerifier = nullVerifier;
    }

    public Task<BlockchainAnchorVerificationResult> VerifyIssueAnchorAsync(
        BlockchainAnchorProof proof,
        string expectedIssuerWalletAddress,
        CancellationToken cancellationToken) =>
        Resolve().VerifyIssueAnchorAsync(proof, expectedIssuerWalletAddress, cancellationToken);

    public Task<BlockchainAnchorVerificationResult> VerifyRevocationAnchorAsync(
        BlockchainAnchorProof proof,
        string expectedIssuerWalletAddress,
        Guid credentialId,
        CancellationToken cancellationToken) =>
        Resolve().VerifyRevocationAnchorAsync(proof, expectedIssuerWalletAddress, credentialId, cancellationToken);

    private IBlockchainAnchorVerifier Resolve() =>
        _options.Blockchain.Enabled && !string.IsNullOrWhiteSpace(_options.Blockchain.RpcUrl)
            ? _rpcVerifier
            : _nullVerifier;
}
