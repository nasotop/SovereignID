using Verifier.Domain;

namespace Verifier.Application;

/// <summary>Resultado del caso de uso: el veredicto más, si existe, la credencial resuelta.</summary>
public sealed record VerificationOutcome(VerificationVerdict Verdict, CredentialReadModel? Credential);

/// <summary>
/// Verifica una credencial por su UUID. Combina chequeos de BD con evidencia externa opcional
/// (on-chain, IPFS, firma EIP-712) y aplica la precedencia
/// <c>not_found &gt; revoked &gt; expired &gt; integrity_failed &gt; valid</c>.
/// </summary>
public sealed class VerifyCredentialUseCase
{
    private const string StatusRevoked = "revoked";
    private const string StatusExpired = "expired";

    private readonly ICredentialReadStore _credentialReadStore;
    private readonly ICredentialEvidenceVerifier _evidenceVerifier;
    private readonly IVerificationLogStore _verificationLogStore;
    private readonly TimeProvider _timeProvider;

    public VerifyCredentialUseCase(
        ICredentialReadStore credentialReadStore,
        ICredentialEvidenceVerifier evidenceVerifier,
        IVerificationLogStore verificationLogStore,
        TimeProvider timeProvider)
    {
        _credentialReadStore = credentialReadStore;
        _evidenceVerifier = evidenceVerifier;
        _verificationLogStore = verificationLogStore;
        _timeProvider = timeProvider;
    }

    public async Task<VerificationOutcome> ExecuteAsync(Guid credentialId, CancellationToken cancellationToken)
    {
        var credential = await _credentialReadStore.GetByIdAsync(credentialId, cancellationToken);

        if (credential is null)
        {
            return await RecordAndReturnAsync(
                credentialId,
                null,
                new VerificationVerdict(
                    VerificationResult.NotFound,
                    new VerificationChecks(
                        Found: false,
                        NotRevoked: null,
                        NotExpired: null,
                        HashMatches: null,
                        OnChainExists: null,
                        SignatureValid: null)),
                cancellationToken);
        }

        var now = _timeProvider.GetUtcNow();
        var bdRevoked = IsBdRevoked(credential);
        var isExpired = IsExpired(credential, now);

        var evidence = await _evidenceVerifier.VerifyAsync(
            new CredentialEvidence(
                credential.Id,
                credential.InstitutionId,
                credential.Anchors.ContentHash,
                credential.Anchors.IpfsCid,
                credential.IpfsGatewayUrl,
                credential.IssuerWalletAddress,
                credential.SubjectWalletAddress,
                credential.Eip712Signature,
                credential.Anchors.ChainId),
            cancellationToken);

        var onChainRevoked = evidence.OnChainRevoked == true;
        var isRevoked = bdRevoked || onChainRevoked;
        var revocationSource = ResolveRevocationSource(bdRevoked, onChainRevoked);

        var checks = new VerificationChecks(
            Found: true,
            NotRevoked: !isRevoked,
            NotExpired: !isExpired,
            HashMatches: evidence.HashMatches,
            OnChainExists: evidence.OnChainExists,
            SignatureValid: evidence.SignatureValid,
            ValidationSource: evidence.ValidationSource,
            RevocationSource: revocationSource);

        var result = ResolveResult(isRevoked, isExpired, checks);

        return await RecordAndReturnAsync(
            credentialId,
            credential,
            new VerificationVerdict(result, checks),
            cancellationToken);
    }

    private async Task<VerificationOutcome> RecordAndReturnAsync(
        Guid credentialId,
        CredentialReadModel? credential,
        VerificationVerdict verdict,
        CancellationToken cancellationToken)
    {
        var checks = verdict.Checks;

        await _verificationLogStore.RecordAsync(
            new VerificationLogEntry(
                CredentialId: credential?.Id,
                CredentialIdQuery: credentialId.ToString(),
                Result: verdict.Result,
                NotRevoked: checks.NotRevoked,
                NotExpired: checks.NotExpired,
                HashMatches: checks.HashMatches,
                OnChainExists: checks.OnChainExists,
                SignatureValid: checks.SignatureValid,
                SignatureValidationSource: checks.ValidationSource,
                RevocationSource: checks.RevocationSource),
            cancellationToken);

        return new VerificationOutcome(verdict, credential);
    }

    private static bool IsBdRevoked(CredentialReadModel credential) =>
        string.Equals(credential.Status, StatusRevoked, StringComparison.OrdinalIgnoreCase)
        || credential.RevokedAt is not null;

    private static bool IsExpired(CredentialReadModel credential, DateTimeOffset now) =>
        (credential.ExpiresAt is { } expiresAt && expiresAt < now)
        || string.Equals(credential.Status, StatusExpired, StringComparison.OrdinalIgnoreCase);

    private static string? ResolveRevocationSource(bool bdRevoked, bool onChainRevoked) => (bdRevoked, onChainRevoked) switch
    {
        (true, true) => RevocationSource.Both.ToWireValue(),
        (true, false) => RevocationSource.Bd.ToWireValue(),
        (false, true) => RevocationSource.OnChain.ToWireValue(),
        _ => null
    };

    private static VerificationResult ResolveResult(bool isRevoked, bool isExpired, VerificationChecks checks)
    {
        if (isRevoked)
        {
            return VerificationResult.Revoked;
        }

        if (isExpired)
        {
            return VerificationResult.Expired;
        }

        if (HasIntegrityFailure(checks))
        {
            return VerificationResult.IntegrityFailed;
        }

        return VerificationResult.Valid;
    }

    private static bool HasIntegrityFailure(VerificationChecks checks) =>
        checks.OnChainExists == false || checks.HashMatches == false || checks.SignatureValid == false;
}
