namespace Auth.Domain;

public sealed class AuthChallenge
{
    private int _consumed;

    public required string Nonce { get; init; }
    public required DateTimeOffset IssuedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }

    public bool Consumed => Volatile.Read(ref _consumed) == 1;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool TryMarkConsumed() =>
        Interlocked.CompareExchange(ref _consumed, 1, 0) == 0;
}
