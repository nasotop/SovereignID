using Auth.Domain;

namespace Auth.Application;

public interface IChallengeStore
{
    void Store(AuthChallenge challenge);

    AuthChallenge? Get(string nonce);

    bool TryConsume(string nonce, out AuthChallenge? challenge);
}
