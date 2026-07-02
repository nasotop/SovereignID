namespace Verifier.Domain;

public static class VerifierErrorCodes
{
    /// <summary><c>credentialId</c> ausente o con formato de UUID inválido (HTTP 400).</summary>
    public const string InvalidCredentialId = "invalid_credential_id";
}
