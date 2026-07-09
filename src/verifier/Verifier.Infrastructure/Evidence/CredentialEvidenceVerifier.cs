using Microsoft.Extensions.Options;
using Verifier.Application;
using Verifier.Domain;
using Verifier.Infrastructure.Blockchain;

namespace Verifier.Infrastructure.Evidence;

internal sealed class CredentialEvidenceVerifier : ICredentialEvidenceVerifier
{
    private readonly EvidenceVerificationOptions _options;
    private readonly ICredentialRegistryReader? _registryReader;
    private readonly IIpfsContentReader? _ipfsReader;
    private readonly IEip712IssuanceSignatureVerifier? _signatureVerifier;

    public CredentialEvidenceVerifier(
        IOptions<VerifierOptions> options,
        ICredentialRegistryReader? registryReader,
        IIpfsContentReader? ipfsReader,
        IEip712IssuanceSignatureVerifier? signatureVerifier)
    {
        _options = options.Value.Evidence;
        _registryReader = registryReader;
        _ipfsReader = ipfsReader;
        _signatureVerifier = signatureVerifier;
    }

    public async Task<CredentialEvidenceChecks> VerifyAsync(CredentialEvidence evidence, CancellationToken cancellationToken)
    {
        var onChainTask = EvaluateOnChainAsync(evidence, cancellationToken);
        var ipfsTask = EvaluateIpfsAsync(evidence, cancellationToken);

        await Task.WhenAll(onChainTask, ipfsTask);

        var (onChainExists, onChainRevoked, onChainIssuer) = await onChainTask;
        var hashMatches = await ipfsTask;
        var (signatureValid, validationSource) = EvaluateSignature(evidence, onChainExists, onChainIssuer);

        return new CredentialEvidenceChecks(
            hashMatches,
            onChainExists,
            onChainRevoked,
            signatureValid,
            validationSource);
    }

    private async Task<(bool? OnChainExists, bool? OnChainRevoked, string? OnChainIssuer)> EvaluateOnChainAsync(
        CredentialEvidence evidence,
        CancellationToken cancellationToken)
    {
        if (!_options.OnChainCheckEnabled || _registryReader is null)
        {
            return (null, null, null);
        }

        var read = await _registryReader.ReadAsync(evidence.CredentialId, cancellationToken);
        if (read.Outcome == RegistryReadOutcome.Unavailable)
        {
            return (null, null, null);
        }

        if (read.Outcome == RegistryReadOutcome.NotFound)
        {
            return (false, null, null);
        }

        var evaluated = OnChainCoherence.Evaluate(read, evidence);
        if (evaluated.Outcome == RegistryReadOutcome.Incoherent)
        {
            return (false, null, null);
        }

        return (true, evaluated.Data?.Revoked, evaluated.Data?.Issuer);
    }

    private async Task<bool?> EvaluateIpfsAsync(CredentialEvidence evidence, CancellationToken cancellationToken)
    {
        if (!_options.IpfsCheckEnabled || _ipfsReader is null)
        {
            return null;
        }

        var result = await _ipfsReader.ReadAndHashAsync(
            evidence.IpfsGatewayUrl,
            evidence.ContentHash,
            cancellationToken);

        return result.HashMatches;
    }

    private (bool? SignatureValid, string? ValidationSource) EvaluateSignature(
        CredentialEvidence evidence,
        bool? onChainExists,
        string? onChainIssuer)
    {
        if (!_options.SignatureCheckEnabled || _signatureVerifier is null)
        {
            return (null, null);
        }

        if (!NethereumEip712IssuanceSignatureVerifier.IsValidSignatureFormat(evidence.Eip712Signature))
        {
            var source = onChainExists == true
                ? SignatureValidationSource.OnChain.ToWireValue()
                : SignatureValidationSource.NotEvaluated.ToWireValue();
            return (false, source);
        }

        var message = new CredentialIssuanceMessage(
            GuidToBytes32.Convert(evidence.CredentialId),
            GuidToBytes32.Convert(evidence.InstitutionId),
            evidence.ContentHash,
            evidence.IpfsCid,
            evidence.SubjectWalletAddress.ToLowerInvariant());

        var verifyingContract = _options.RegistryAddress;
        if (!_signatureVerifier.TryRecoverSigner(
                message,
                evidence.Eip712Signature!,
                _options.ChainId,
                verifyingContract,
                out var recovered))
        {
            return (false, onChainExists == true
                ? SignatureValidationSource.OnChain.ToWireValue()
                : SignatureValidationSource.BdFallbackRejected.ToWireValue());
        }

        if (onChainExists == true && !string.IsNullOrWhiteSpace(onChainIssuer))
        {
            var matches = string.Equals(recovered, onChainIssuer, StringComparison.OrdinalIgnoreCase);
            return (matches, SignatureValidationSource.OnChain.ToWireValue());
        }

        if (string.IsNullOrWhiteSpace(evidence.IssuerWalletAddress))
        {
            return (null, SignatureValidationSource.NotEvaluated.ToWireValue());
        }

        if (string.Equals(recovered, evidence.IssuerWalletAddress, StringComparison.OrdinalIgnoreCase))
        {
            return (null, SignatureValidationSource.BdFallbackInconclusive.ToWireValue());
        }

        return (false, SignatureValidationSource.BdFallbackRejected.ToWireValue());
    }
}

internal sealed class NullCredentialEvidenceVerifier : ICredentialEvidenceVerifier
{
    public Task<CredentialEvidenceChecks> VerifyAsync(CredentialEvidence evidence, CancellationToken cancellationToken) =>
        Task.FromResult(new CredentialEvidenceChecks(null, null, null, null, null));
}

internal sealed class ConfigurableCredentialEvidenceVerifier : ICredentialEvidenceVerifier
{
    private readonly VerifierOptions _options;
    private readonly CredentialEvidenceVerifier _liveVerifier;
    private readonly NullCredentialEvidenceVerifier _nullVerifier;

    public ConfigurableCredentialEvidenceVerifier(
        IOptions<VerifierOptions> options,
        CredentialEvidenceVerifier liveVerifier,
        NullCredentialEvidenceVerifier nullVerifier)
    {
        _options = options.Value;
        _liveVerifier = liveVerifier;
        _nullVerifier = nullVerifier;
    }

    public Task<CredentialEvidenceChecks> VerifyAsync(CredentialEvidence evidence, CancellationToken cancellationToken)
    {
        var evidenceEnabled = _options.Evidence.OnChainCheckEnabled
            || _options.Evidence.IpfsCheckEnabled
            || _options.Evidence.SignatureCheckEnabled;

        return evidenceEnabled
            ? _liveVerifier.VerifyAsync(evidence, cancellationToken)
            : _nullVerifier.VerifyAsync(evidence, cancellationToken);
    }
}
