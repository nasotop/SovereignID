using Auth.Domain;
using Microsoft.Extensions.Options;

namespace Auth.Application;

public sealed class VerifySiweUseCase
{
    private readonly ISiweMessageParser _parser;
    private readonly IChallengeStore _challengeStore;
    private readonly ISignatureVerifier _signatureVerifier;
    private readonly IJwtTokenIssuer _jwtTokenIssuer;
    private readonly TimeProvider _timeProvider;
    private readonly AuthOptions _options;

    public VerifySiweUseCase(
        ISiweMessageParser parser,
        IChallengeStore challengeStore,
        ISignatureVerifier signatureVerifier,
        IJwtTokenIssuer jwtTokenIssuer,
        TimeProvider timeProvider,
        IOptions<AuthOptions> options)
    {
        _parser = parser;
        _challengeStore = challengeStore;
        _signatureVerifier = signatureVerifier;
        _jwtTokenIssuer = jwtTokenIssuer;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public VerifySiweResult Execute(string message, string signature)
    {
        var parseResult = _parser.TryParse(message);
        if (!parseResult.IsSuccess)
        {
            return Fail(
                AuthErrorCodes.SiweParseFailed,
                400,
                parseResult.ErrorDetail ?? "SIWE message is malformed.");
        }

        var siwe = parseResult.Message!;
        var challenge = _challengeStore.Get(siwe.Nonce);
        if (challenge is null)
        {
            return Fail(AuthErrorCodes.NonceUnknown, 401, "Nonce was not issued by this backend.");
        }

        var now = _timeProvider.GetUtcNow();
        if (challenge.IsExpired(now))
        {
            return Fail(AuthErrorCodes.NonceExpired, 401, "Auth challenge has expired.");
        }

        if (challenge.Consumed)
        {
            return Fail(AuthErrorCodes.NonceConsumed, 401, "Auth challenge was already consumed.");
        }

        if (siwe.ChainId != _options.AllowedChainId)
        {
            return Fail(
                AuthErrorCodes.UnsupportedChain,
                400,
                $"Chain ID {siwe.ChainId} is not supported. Expected {_options.AllowedChainId}.");
        }

        if (!_signatureVerifier.TryRecoverAddress(siwe.OriginalPayload, signature, out var recoveredAddress)
            || !AddressesMatch(siwe.Address, recoveredAddress))
        {
            return Fail(AuthErrorCodes.SignatureMismatch, 401, "Signature does not match the declared address.");
        }

        if (!_challengeStore.TryConsume(siwe.Nonce, out _))
        {
            return Fail(AuthErrorCodes.NonceConsumed, 401, "Auth challenge was already consumed.");
        }

        var token = _jwtTokenIssuer.Issue(recoveredAddress);
        return new VerifySiweSuccess(token.Token, recoveredAddress, token.ExpiresAt);
    }

    private static bool AddressesMatch(string declared, string recovered) =>
        string.Equals(declared, recovered, StringComparison.OrdinalIgnoreCase);

    private static VerifySiweFailure Fail(string errorCode, int statusCode, string detail) =>
        new(new AuthFailure(errorCode, statusCode, detail));
}
