using Verifier.Domain;

namespace Verifier.Application;

/// <summary>
/// Intento de verificación a registrar en <c>verification_logs</c>.
/// Los sub-booleanos no evaluados (<c>signature_valid</c>, <c>hash_matches</c>, <c>on_chain_exists</c>)
/// se persisten como <c>NULL</c>. En <c>not_found</c>, <see cref="CredentialId"/> es <c>null</c>.
/// </summary>
public sealed record VerificationLogEntry(
    Guid? CredentialId,
    string CredentialIdQuery,
    VerificationResult Result,
    bool? NotRevoked,
    bool? NotExpired);

/// <summary>Escritura del intento de verificación.</summary>
public interface IVerificationLogStore
{
    Task RecordAsync(VerificationLogEntry entry, CancellationToken cancellationToken);
}
