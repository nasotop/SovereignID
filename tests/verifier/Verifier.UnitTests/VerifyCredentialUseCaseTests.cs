using Verifier.Application;
using Verifier.Domain;

namespace Verifier.UnitTests;

public sealed class VerifyCredentialUseCaseTests
{
    private static readonly Guid CredentialId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task ReturnsIntegrityFailed_WhenSignatureInvalid()
    {
        var credential = CreateCredential();
        var useCase = CreateUseCase(
            credential,
            new CredentialEvidenceChecks(null, null, null, false, "on_chain"));

        var outcome = await useCase.ExecuteAsync(CredentialId, CancellationToken.None);

        Assert.Equal(VerificationResult.IntegrityFailed, outcome.Verdict.Result);
        Assert.Equal("integrity_failed", outcome.Verdict.Result.ToWireValue());
    }

    [Fact]
    public async Task RevokedTakesPrecedenceOverIntegrityFailed()
    {
        var credential = CreateCredential(status: "revoked");
        var useCase = CreateUseCase(
            credential,
            new CredentialEvidenceChecks(false, null, null, false, "on_chain"));

        var outcome = await useCase.ExecuteAsync(CredentialId, CancellationToken.None);

        Assert.Equal(VerificationResult.Revoked, outcome.Verdict.Result);
    }

    [Fact]
    public async Task ExpiredTakesPrecedenceOverIntegrityFailed()
    {
        var credential = CreateCredential(expiresAt: DateTimeOffset.UtcNow.AddDays(-1));
        var useCase = CreateUseCase(
            credential,
            new CredentialEvidenceChecks(false, null, null, false, "on_chain"));

        var outcome = await useCase.ExecuteAsync(CredentialId, CancellationToken.None);

        Assert.Equal(VerificationResult.Expired, outcome.Verdict.Result);
    }

    [Fact]
    public async Task ReturnsValid_WhenEvidenceChecksAreNull()
    {
        var credential = CreateCredential();
        var useCase = CreateUseCase(
            credential,
            new CredentialEvidenceChecks(null, null, null, null, null));

        var outcome = await useCase.ExecuteAsync(CredentialId, CancellationToken.None);

        Assert.Equal(VerificationResult.Valid, outcome.Verdict.Result);
    }

    [Fact]
    public async Task RevocationSource_IsOnChain_WhenOnlyOnChainRevoked()
    {
        var credential = CreateCredential();
        var useCase = CreateUseCase(
            credential,
            new CredentialEvidenceChecks(null, true, true, null, null));

        var outcome = await useCase.ExecuteAsync(CredentialId, CancellationToken.None);

        Assert.Equal(VerificationResult.Revoked, outcome.Verdict.Result);
        Assert.Equal("on_chain", outcome.Verdict.Checks.RevocationSource);
    }

    private static VerifyCredentialUseCase CreateUseCase(
        CredentialReadModel? credential,
        CredentialEvidenceChecks evidenceChecks) =>
        new(
            new FakeCredentialReadStore(credential),
            new FakeEvidenceVerifier(evidenceChecks),
            new FakeVerificationLogStore(),
            TimeProvider.System);

    private static CredentialReadModel CreateCredential(
        string status = "active",
        DateTimeOffset? expiresAt = null) =>
        new(
            CredentialId,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "diploma",
            status,
            DateTimeOffset.UtcNow.AddYears(-1),
            expiresAt,
            null,
            "did:ethr:sepolia:0xsubject",
            "0xIssuer00000000000000000000000000000001",
            "0xSubject00000000000000000000000000000002",
            "0x" + new string('a', 130),
            "https://ipfs.example/cid",
            new IssuerReadModel("did:ethr:sepolia:0xissuer", "Demo", "DEMO"),
            new CredentialAnchors("bafytest", "0x" + new string('b', 64), "0x" + new string('c', 64), 11155111));

    private sealed class FakeCredentialReadStore(CredentialReadModel? credential) : ICredentialReadStore
    {
        public Task<CredentialReadModel?> GetByIdAsync(Guid credentialId, CancellationToken cancellationToken) =>
            Task.FromResult(credential);
    }

    private sealed class FakeEvidenceVerifier(CredentialEvidenceChecks checks) : ICredentialEvidenceVerifier
    {
        public Task<CredentialEvidenceChecks> VerifyAsync(CredentialEvidence evidence, CancellationToken cancellationToken) =>
            Task.FromResult(checks);
    }

    private sealed class FakeVerificationLogStore : IVerificationLogStore
    {
        public VerificationLogEntry? LastEntry { get; private set; }

        public Task RecordAsync(VerificationLogEntry entry, CancellationToken cancellationToken)
        {
            LastEntry = entry;
            return Task.CompletedTask;
        }
    }
}
