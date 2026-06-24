using System.Collections.Concurrent;
using Auth.Application;
using Auth.Domain;

namespace Auth.Infrastructure.Persistence.Stores.ChallengeStore;

internal sealed class InMemoryChallengeStore : IChallengeStore
{
    private readonly ConcurrentDictionary<string, AuthChallenge> _challenges = new();

    public void Store(AuthChallenge challenge) =>
        _challenges[challenge.Nonce] = challenge;

    public AuthChallenge? Get(string nonce) =>
        _challenges.TryGetValue(nonce, out var challenge) ? challenge : null;

    public bool TryConsume(string nonce, out AuthChallenge? challenge)
    {
        challenge = null;

        if (!_challenges.TryGetValue(nonce, out var current))
        {
            return false;
        }

        if (current.TryMarkConsumed())
        {
            challenge = current;
            return true;
        }

        challenge = current;
        return false;
    }
}
