using Auth.Application;
using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DomainAuthChallenge = Auth.Domain.AuthChallenge;
using AuthChallengeRow = Auth.Infrastructure.Persistence.Entities.AuthChallenge;

namespace Auth.Infrastructure.Persistence;

internal sealed class PostgresChallengeStore : IChallengeStore
{
    private readonly SovereignIdDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly AuthOptions _options;

    public PostgresChallengeStore(
        SovereignIdDbContext db,
        TimeProvider timeProvider,
        IOptions<AuthOptions> options)
    {
        _db = db;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public void Store(DomainAuthChallenge challenge)
    {
        _db.AuthChallenges.Add(new AuthChallengeRow
        {
            Id = Guid.NewGuid(),
            Nonce = challenge.Nonce,
            IssuedAt = ToUtcDateTime(challenge.IssuedAt),
            ExpiresAt = ToUtcDateTime(challenge.ExpiresAt),
            ChainId = _options.AllowedChainId,
            ConsumedAt = null
        });

        _db.SaveChanges();
    }

    public DomainAuthChallenge? Get(string nonce)
    {
        var row = _db.AuthChallenges
            .AsNoTracking()
            .FirstOrDefault(c => c.Nonce == nonce);

        return row is null ? null : ToDomain(row);
    }

    public bool TryConsume(string nonce, out DomainAuthChallenge? challenge)
    {
        challenge = null;
        var consumedAt = ToUtcDateTime(_timeProvider.GetUtcNow());

        var updated = _db.AuthChallenges
            .Where(c => c.Nonce == nonce && c.ConsumedAt == null)
            .ExecuteUpdate(setters => setters.SetProperty(c => c.ConsumedAt, consumedAt));

        if (updated == 1)
        {
            challenge = Get(nonce);
            return challenge is not null;
        }

        var existing = Get(nonce);
        challenge = existing;
        return false;
    }

    private static DomainAuthChallenge ToDomain(AuthChallengeRow row) =>
        DomainAuthChallenge.Rehydrate(
            row.Nonce,
            ToUtcOffset(row.IssuedAt),
            ToUtcOffset(row.ExpiresAt),
            row.ConsumedAt is not null);

    private static DateTime ToUtcDateTime(DateTimeOffset value) =>
        value.UtcDateTime;

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
