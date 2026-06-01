using System.Security.Cryptography;
using Auth.Domain;
using Microsoft.Extensions.Options;

namespace Auth.Application;

public sealed class IssueNonceUseCase
{
    private readonly IChallengeStore _challengeStore;
    private readonly TimeProvider _timeProvider;
    private readonly AuthOptions _options;

    public IssueNonceUseCase(
        IChallengeStore challengeStore,
        TimeProvider timeProvider,
        IOptions<AuthOptions> options)
    {
        _challengeStore = challengeStore;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public IssueNonceResult Execute()
    {
        var issuedAt = _timeProvider.GetUtcNow();
        var expiresAt = issuedAt.AddSeconds(_options.ChallengeTtlSeconds);
        var nonce = GenerateNonce();

        var challenge = new AuthChallenge
        {
            Nonce = nonce,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt
        };

        _challengeStore.Store(challenge);

        return new IssueNonceResult(nonce, expiresAt);
    }

    private static string GenerateNonce()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
