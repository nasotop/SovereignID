namespace Auth.IntegrationTests;

public sealed class ControllableTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

    public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;

    public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);

    public override DateTimeOffset GetUtcNow() => _utcNow;
}
