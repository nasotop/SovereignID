namespace Auth.Domain;

public static class AuthErrorCodes
{
    public const string SiweParseFailed = "siwe_parse_failed";
    public const string UnsupportedChain = "unsupported_chain";
    public const string NonceUnknown = "nonce_unknown";
    public const string NonceExpired = "nonce_expired";
    public const string NonceConsumed = "nonce_consumed";
    public const string SignatureMismatch = "signature_mismatch";
}
