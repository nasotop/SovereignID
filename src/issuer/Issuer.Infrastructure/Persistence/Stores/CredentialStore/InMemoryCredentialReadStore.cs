using Issuer.Application;

namespace Issuer.Infrastructure.Persistence.Stores.CredentialStore;

internal sealed class InMemoryCredentialReadStore : ICredentialReadStore
{
    public static readonly string HolderSubjectDid =
        "did:ethr:sepolia:0x2222222222222222222222222222222222222222";

    private static readonly Guid CredentialOneId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CredentialTwoId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static readonly IReadOnlyList<HolderCredentialSummary> Summaries =
    [
        new HolderCredentialSummary(
            CredentialOneId,
            "Titulo Universitario",
            "Duoc UC",
            "TITULO",
            "active",
            new DateTimeOffset(2025, 11, 15, 0, 0, 0, TimeSpan.Zero),
            null),
        new HolderCredentialSummary(
            CredentialTwoId,
            "Certificado de Notas",
            "Duoc UC",
            "NOTAS",
            "active",
            new DateTimeOffset(2025, 10, 22, 0, 0, 0, TimeSpan.Zero),
            null)
    ];

    private static readonly IReadOnlyDictionary<Guid, HolderCredentialDetail> Details =
        Summaries.ToDictionary(
            summary => summary.Id,
            summary => new HolderCredentialDetail(
                summary.Id,
                summary.Title,
                summary.IssuerName,
                summary.TypeCode,
                summary.Status,
                summary.IssuedAt,
                summary.ExpiresAt,
                RevokedAt: null,
                HolderSubjectDid,
                new HolderCredentialIssuer(
                    "did:ethr:sepolia:0x1111111111111111111111111111111111111111",
                    summary.IssuerName,
                    "DUOC"),
                new HolderCredentialAnchors(
                    "bafybeigdyrzt",
                    "https://ipfs.io/ipfs/bafybeigdyrzt",
                    "0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                    "0xbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                    123456L,
                    11155111,
                    "0xcccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"),
                null));

    public Task<IReadOnlyList<HolderCredentialSummary>> ListBySubjectDidAsync(
        string subjectDid,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(subjectDid, HolderSubjectDid, StringComparison.Ordinal))
        {
            return Task.FromResult<IReadOnlyList<HolderCredentialSummary>>([]);
        }

        return Task.FromResult(Summaries);
    }

    public Task<HolderCredentialDetail?> GetByIdForSubjectAsync(
        Guid credentialId,
        string subjectDid,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(subjectDid, HolderSubjectDid, StringComparison.Ordinal))
        {
            return Task.FromResult<HolderCredentialDetail?>(null);
        }

        Details.TryGetValue(credentialId, out var detail);
        return Task.FromResult(detail);
    }
}
