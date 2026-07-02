using Microsoft.EntityFrameworkCore;
using Verifier.Application;
using Verifier.Infrastructure.Persistence.Generated;
using Verifier.Infrastructure.Persistence.Generated.Entities;

namespace Verifier.Infrastructure.Persistence.Stores.CredentialStore;

internal sealed class EfCredentialReadStore : ICredentialReadStore
{
    private readonly VerifierDbContext _db;

    public EfCredentialReadStore(VerifierDbContext db) => _db = db;

    public async Task<CredentialReadModel?> GetByIdAsync(Guid credentialId, CancellationToken cancellationToken)
    {
        var row = await _db.Credentials
            .AsNoTracking()
            .Where(c => c.Id == credentialId)
            .Select(c => new
            {
                c.Id,
                TypeCode = c.CredentialType.Code,
                c.Status,
                c.IssuedAt,
                c.ExpiresAt,
                c.RevokedAt,
                c.SubjectDid,
                IssuerDid = c.Institution.Did,
                IssuerDisplayName = c.Institution.DisplayName,
                IssuerCode = c.Institution.Code,
                c.IpfsCid,
                c.ContentHash,
                c.TransactionHash,
                c.ChainId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        return new CredentialReadModel(
            row.Id,
            row.TypeCode,
            ToStatusWireValue(row.Status),
            ToUtcOffset(row.IssuedAt),
            ToUtcOffset(row.ExpiresAt),
            ToUtcOffset(row.RevokedAt),
            row.SubjectDid,
            new IssuerReadModel(row.IssuerDid, row.IssuerDisplayName, row.IssuerCode),
            new CredentialAnchors(row.IpfsCid, row.ContentHash, row.TransactionHash, row.ChainId));
    }

    private static string ToStatusWireValue(CredentialStatus status) => status switch
    {
        CredentialStatus.Active => "active",
        CredentialStatus.Revoked => "revoked",
        CredentialStatus.Expired => "expired",
        _ => status.ToString().ToLowerInvariant()
    };

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static DateTimeOffset? ToUtcOffset(DateTime? value) =>
        value is null ? null : ToUtcOffset(value.Value);
}
