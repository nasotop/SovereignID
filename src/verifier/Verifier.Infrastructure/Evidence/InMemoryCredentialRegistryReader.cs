using Verifier.Application;
using Verifier.Infrastructure.Blockchain;

namespace Verifier.Infrastructure.Evidence;

internal sealed class InMemoryCredentialRegistryReader : ICredentialRegistryReader
{
    private readonly Dictionary<Guid, RegistryReadResult> _scenarios = new();

    public void SetScenario(Guid credentialId, RegistryReadResult result) =>
        _scenarios[credentialId] = result;

    public Task<RegistryReadResult> ReadAsync(Guid credentialId, CancellationToken cancellationToken)
    {
        if (_scenarios.TryGetValue(credentialId, out var result))
        {
            return Task.FromResult(result);
        }

        return Task.FromResult(new RegistryReadResult(RegistryReadOutcome.NotFound, null));
    }

    public static OnChainCredentialData CreateCoherentData(CredentialEvidence evidence, bool revoked = false) =>
        new(
            evidence.ContentHash,
            evidence.IpfsCid,
            GuidToBytes32.Convert(evidence.InstitutionId),
            (evidence.IssuerWalletAddress ?? string.Empty).ToLowerInvariant(),
            evidence.SubjectWalletAddress.ToLowerInvariant(),
            revoked);

    public static OnChainCredentialData CreateIncoherentData(CredentialEvidence evidence) =>
        CreateCoherentData(evidence) with { ContentHash = "0x" + new string('a', 64) };
}
