namespace Verifier.Domain;

public enum SignatureValidationSource
{
    OnChain,
    BdFallbackInconclusive,
    BdFallbackRejected,
    NotEvaluated
}

public static class SignatureValidationSourceExtensions
{
    public static string ToWireValue(this SignatureValidationSource source) => source switch
    {
        SignatureValidationSource.OnChain => "on_chain",
        SignatureValidationSource.BdFallbackInconclusive => "bd_fallback_inconclusive",
        SignatureValidationSource.BdFallbackRejected => "bd_fallback_rejected",
        SignatureValidationSource.NotEvaluated => "not_evaluated",
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown signature validation source.")
    };
}
