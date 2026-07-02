using Verifier.Domain;

namespace Verifier.Application;

/// <summary>Resultado del caso de uso: el veredicto más, si existe, la credencial resuelta.</summary>
public sealed record VerificationOutcome(VerificationVerdict Verdict, CredentialReadModel? Credential);

/// <summary>
/// Verifica una credencial por su UUID. Computa los chequeos sin dependencia externa
/// (<c>found</c>, <c>notRevoked</c>, <c>notExpired</c>), aplica la precedencia
/// <c>not_found &gt; revoked &gt; expired &gt; valid</c> y registra el intento en <c>verification_logs</c>.
/// </summary>
public sealed class VerifyCredentialUseCase
{
    private const string StatusRevoked = "revoked";
    private const string StatusExpired = "expired";

    private readonly ICredentialReadStore _credentialReadStore;
    private readonly IVerificationLogStore _verificationLogStore;
    private readonly TimeProvider _timeProvider;

    public VerifyCredentialUseCase(
        ICredentialReadStore credentialReadStore,
        IVerificationLogStore verificationLogStore,
        TimeProvider timeProvider)
    {
        _credentialReadStore = credentialReadStore;
        _verificationLogStore = verificationLogStore;
        _timeProvider = timeProvider;
    }

    public async Task<VerificationOutcome> ExecuteAsync(Guid credentialId, CancellationToken cancellationToken)
    {
        var credential = await _credentialReadStore.GetByIdAsync(credentialId, cancellationToken);

        if (credential is null)
        {
            var notFoundVerdict = new VerificationVerdict(
                VerificationResult.NotFound,
                new VerificationChecks(
                    Found: false,
                    NotRevoked: null,
                    NotExpired: null,
                    HashMatches: null,
                    OnChainExists: null,
                    SignatureValid: null));

            await _verificationLogStore.RecordAsync(
                new VerificationLogEntry(
                    CredentialId: null,
                    CredentialIdQuery: credentialId.ToString(),
                    Result: VerificationResult.NotFound,
                    NotRevoked: null,
                    NotExpired: null),
                cancellationToken);

            return new VerificationOutcome(notFoundVerdict, Credential: null);
        }

        var now = _timeProvider.GetUtcNow();

        var isRevoked = string.Equals(credential.Status, StatusRevoked, StringComparison.OrdinalIgnoreCase)
            || credential.RevokedAt is not null;

        var isExpired = (credential.ExpiresAt is { } expiresAt && expiresAt < now)
            || string.Equals(credential.Status, StatusExpired, StringComparison.OrdinalIgnoreCase);

        var result = isRevoked
            ? VerificationResult.Revoked
            : isExpired
                ? VerificationResult.Expired
                : VerificationResult.Valid;

        var verdict = new VerificationVerdict(
            result,
            new VerificationChecks(
                Found: true,
                NotRevoked: !isRevoked,
                NotExpired: !isExpired,
                HashMatches: null,
                OnChainExists: null,
                SignatureValid: null));

        await _verificationLogStore.RecordAsync(
            new VerificationLogEntry(
                CredentialId: credential.Id,
                CredentialIdQuery: credentialId.ToString(),
                Result: result,
                NotRevoked: !isRevoked,
                NotExpired: !isExpired),
            cancellationToken);

        return new VerificationOutcome(verdict, credential);
    }
}
