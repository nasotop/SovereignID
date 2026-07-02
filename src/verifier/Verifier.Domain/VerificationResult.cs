namespace Verifier.Domain;

/// <summary>
/// Veredicto resumido de una verificación, emitido en v1.
/// Los valores <c>invalid_signature</c>, <c>tampered</c> e <c>ipfs_unreachable</c> quedan reservados
/// en el contrato y no se emiten en v1.
/// </summary>
public enum VerificationResult
{
    Valid,
    Revoked,
    Expired,
    NotFound
}

public static class VerificationResultExtensions
{
    /// <summary>Devuelve el valor estable (snake_case) usado en el contrato HTTP y en <c>verification_logs.result</c>.</summary>
    public static string ToWireValue(this VerificationResult result) => result switch
    {
        VerificationResult.Valid => "valid",
        VerificationResult.Revoked => "revoked",
        VerificationResult.Expired => "expired",
        VerificationResult.NotFound => "not_found",
        _ => throw new ArgumentOutOfRangeException(nameof(result), result, "Unknown verification result.")
    };
}
