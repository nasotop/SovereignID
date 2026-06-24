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

    /// <summary>
    /// Rehidrata un auth challenge desde persistencia (p. ej. fila <c>auth_challenges</c>).
    /// </summary>
    public static AuthChallenge Rehydrate(
        string nonce,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        bool consumed)
    {
        var challenge = new AuthChallenge
        {
            Nonce = nonce,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt
        };

        if (consumed)
        {
            challenge.TryMarkConsumed();
        }

        return challenge;
    }
}
