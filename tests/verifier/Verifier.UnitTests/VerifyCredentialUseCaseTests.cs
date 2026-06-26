using Microsoft.Extensions.Time.Testing;
using Verifier.Application;
using Verifier.Domain;

namespace Verifier.UnitTests;

public sealed class VerifyCredentialUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 06, 25, 12, 00, 00, TimeSpan.Zero);

    private static CredentialReadModel Credential(
        string status = "active",
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? revokedAt = null) =>
        new(
            Id: Guid.NewGuid(),
            TypeCode: "TITULO",
            Status: status,
            IssuedAt: Now.AddYears(-1),
            ExpiresAt: expiresAt,
            RevokedAt: revokedAt,
            SubjectDid: "did:ethr:sepolia:0xabc",
            Issuer: new IssuerReadModel("did:ethr:sepolia:0xdef", "Universidad de Ejemplo", "UDE"),
            Anchors: new CredentialAnchors("bafy", "0xhash", "0xtx", 11155111));

    private static (VerifyCredentialUseCase UseCase, RecordingVerificationLogStore Logs) Build(CredentialReadModel? credential)
    {
        var logs = new RecordingVerificationLogStore();
        var timeProvider = new FakeTimeProvider(Now);
        var useCase = new VerifyCredentialUseCase(new FakeCredentialReadStore(credential), logs, timeProvider);
        return (useCase, logs);
    }

    [Fact]
    public async Task ValidCredential_ReturnsValid()
    {
        var (useCase, logs) = Build(Credential());

        var outcome = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(VerificationResult.Valid, outcome.Verdict.Result);
        Assert.True(outcome.Verdict.Checks.Found);
        Assert.True(outcome.Verdict.Checks.NotRevoked);
        Assert.True(outcome.Verdict.Checks.NotExpired);
        Assert.NotNull(outcome.Credential);
        Assert.Single(logs.Entries);
    }

    [Fact]
    public async Task ExternalChecks_AreNotEvaluated()
    {
        var (useCase, _) = Build(Credential());

        var checks = (await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None)).Verdict.Checks;

        Assert.Null(checks.HashMatches);
        Assert.Null(checks.OnChainExists);
        Assert.Null(checks.SignatureValid);
    }

    [Fact]
    public async Task RevokedByStatus_ReturnsRevoked()
    {
        var (useCase, _) = Build(Credential(status: "revoked"));

        var outcome = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(VerificationResult.Revoked, outcome.Verdict.Result);
        Assert.False(outcome.Verdict.Checks.NotRevoked);
    }

    [Fact]
    public async Task RevokedByRevokedAt_ReturnsRevoked()
    {
        var (useCase, _) = Build(Credential(revokedAt: Now.AddDays(-1)));

        var outcome = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(VerificationResult.Revoked, outcome.Verdict.Result);
    }

    [Fact]
    public async Task ExpiredByDate_EvenIfStatusActive_ReturnsExpired()
    {
        var (useCase, _) = Build(Credential(status: "active", expiresAt: Now.AddDays(-1)));

        var outcome = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(VerificationResult.Expired, outcome.Verdict.Result);
        Assert.False(outcome.Verdict.Checks.NotExpired);
    }

    [Fact]
    public async Task NotYetExpired_ReturnsValid()
    {
        var (useCase, _) = Build(Credential(expiresAt: Now.AddDays(1)));

        var outcome = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(VerificationResult.Valid, outcome.Verdict.Result);
    }

    [Fact]
    public async Task RevokedAndExpired_RevokedTakesPrecedence()
    {
        var (useCase, _) = Build(Credential(status: "active", expiresAt: Now.AddDays(-1), revokedAt: Now.AddDays(-2)));

        var outcome = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(VerificationResult.Revoked, outcome.Verdict.Result);
        Assert.False(outcome.Verdict.Checks.NotRevoked);
        Assert.False(outcome.Verdict.Checks.NotExpired);
    }

    [Fact]
    public async Task NotFound_ReturnsNotFound_AndLogsWithNullCredentialId()
    {
        var (useCase, logs) = Build(credential: null);
        var queried = Guid.NewGuid();

        var outcome = await useCase.ExecuteAsync(queried, CancellationToken.None);

        Assert.Equal(VerificationResult.NotFound, outcome.Verdict.Result);
        Assert.False(outcome.Verdict.Checks.Found);
        Assert.Null(outcome.Credential);

        var entry = Assert.Single(logs.Entries);
        Assert.Null(entry.CredentialId);
        Assert.Equal(queried.ToString(), entry.CredentialIdQuery);
        Assert.Equal(VerificationResult.NotFound, entry.Result);
    }
}
