namespace Verifier.Domain;

public static class VerifierErrorCodes
{
    /// <summary><c>credentialId</c> ausente o con formato de UUID inválido (HTTP 400).</summary>
    public const string InvalidCredentialId = "invalid_credential_id";

    /// <summary>Límite de tasa excedido en <c>POST /verifications</c> (HTTP 429).</summary>
    public const string RateLimitExceeded = "rate_limit_exceeded";
}
