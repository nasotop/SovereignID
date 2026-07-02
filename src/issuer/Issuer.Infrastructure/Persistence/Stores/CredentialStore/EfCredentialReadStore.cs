using Issuer.Application;
using Issuer.Infrastructure.Persistence.Generated;
using Issuer.Infrastructure.Persistence.Generated.Entities;
using Microsoft.EntityFrameworkCore;

namespace Issuer.Infrastructure.Persistence.Stores.CredentialStore;

internal sealed class EfCredentialReadStore : ICredentialReadStore
{
    private readonly IssuerDbContext _db;

    public EfCredentialReadStore(IssuerDbContext db) => _db = db;

    public async Task<IReadOnlyList<HolderCredentialSummary>> ListBySubjectDidAsync(
        string subjectDid,
        CancellationToken cancellationToken)
    {
        return await _db.Credentials
            .AsNoTracking()
            .Where(c => c.SubjectDid == subjectDid)
            .OrderByDescending(c => c.IssuedAt)
            .Select(c => new HolderCredentialSummary(
                c.Id,
                c.CredentialType.Name,
                c.Institution.DisplayName,
                c.CredentialType.Code,
                ToStatusWireValue(c.Status),
                ToUtcOffset(c.IssuedAt),
                ToUtcOffset(c.ExpiresAt)))
            .ToListAsync(cancellationToken);
    }

    public async Task<HolderCredentialDetail?> GetByIdForSubjectAsync(
        Guid credentialId,
        string subjectDid,
        CancellationToken cancellationToken)
    {
        var row = await _db.Credentials
            .AsNoTracking()
            .Where(c => c.Id == credentialId && c.SubjectDid == subjectDid)
            .Select(c => new
            {
                c.Id,
                Title = c.CredentialType.Name,
                IssuerName = c.Institution.DisplayName,
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
                c.IpfsGatewayUrl,
                c.ContentHash,
                c.TransactionHash,
                c.BlockNumber,
                c.ChainId,
                c.Eip712Signature,
                c.Metadata
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        return new HolderCredentialDetail(
            row.Id,
            row.Title,
            row.IssuerName,
            row.TypeCode,
            ToStatusWireValue(row.Status),
            ToUtcOffset(row.IssuedAt),
            ToUtcOffset(row.ExpiresAt),
            ToUtcOffset(row.RevokedAt),
            row.SubjectDid,
            new HolderCredentialIssuer(row.IssuerDid, row.IssuerDisplayName, row.IssuerCode),
            new HolderCredentialAnchors(
                row.IpfsCid,
                row.IpfsGatewayUrl,
                row.ContentHash,
                row.TransactionHash,
                row.BlockNumber,
                row.ChainId,
                row.Eip712Signature),
            row.Metadata);
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
