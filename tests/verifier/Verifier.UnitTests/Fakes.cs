using Verifier.Application;

namespace Verifier.UnitTests;

internal sealed class FakeCredentialReadStore : ICredentialReadStore
{
    private readonly CredentialReadModel? _credential;

    public FakeCredentialReadStore(CredentialReadModel? credential) => _credential = credential;

    public Task<CredentialReadModel?> GetByIdAsync(Guid credentialId, CancellationToken cancellationToken) =>
        Task.FromResult(_credential);
}

internal sealed class RecordingVerificationLogStore : IVerificationLogStore
{
    public List<VerificationLogEntry> Entries { get; } = [];

    public Task RecordAsync(VerificationLogEntry entry, CancellationToken cancellationToken)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}
