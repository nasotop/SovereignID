using System.Text.Json;
using Issuer.Application;
using Issuer.Infrastructure.Persistence.Entities;

namespace Issuer.Infrastructure.Persistence.Stores;

internal sealed class InMemoryTitleIssuerRepository : ITitleIssuerRepository
{
    public static readonly Guid InstitutionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid StudentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid CareerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid WalletId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly object Sync = new();
    private static readonly List<CredentialEntity> Credentials = [];
    private static string? _issuerWalletAddress = "0x1111111111111111111111111111111111111111";

    public Task<InstitutionIssuerWalletLinked?> LinkInstitutionIssuerWalletAsync(
        LinkInstitutionIssuerWalletCommand command,
        CancellationToken cancellationToken)
    {
        if (command.InstitutionId != InstitutionId)
        {
            return Task.FromResult<InstitutionIssuerWalletLinked?>(null);
        }

        lock (Sync)
        {
            _issuerWalletAddress = command.WalletAddress;
        }

        return Task.FromResult<InstitutionIssuerWalletLinked?>(new InstitutionIssuerWalletLinked(
            InstitutionId,
            command.WalletAddress,
            command.Did,
            command.PublicKey));
    }

    public Task<StudentTitleLinked?> LinkStudentTitleAsync(
        LinkStudentTitleCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (command.StudentId != StudentId
            || command.CareerId != CareerId
            || !string.Equals(command.CredentialTypeCode, "TITULO", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<StudentTitleLinked?>(null);
        }

        var credentialId = command.CredentialId ?? Guid.NewGuid();
        var entity = new CredentialEntity
        {
            Id = credentialId,
            InstitutionId = InstitutionId,
            CredentialTypeId = 1,
            StudentId = StudentId,
            CareerId = CareerId,
            IssuedToWalletId = WalletId,
            SubjectDid = "did:ethr:sepolia:0x2222222222222222222222222222222222222222",
            IssuerDid = "did:ethr:sepolia:0x1111111111111111111111111111111111111111",
            IpfsCid = command.IpfsCid,
            IpfsGatewayUrl = command.IpfsGatewayUrl,
            ContentHash = command.ContentHash,
            TransactionHash = command.TransactionHash,
            BlockNumber = command.BlockNumber,
            ChainId = command.ChainId ?? 11155111,
            Eip712Signature = command.Eip712Signature,
            Status = CredentialStatus.active,
            IssuedAt = now.UtcDateTime,
            CreatedAt = now.UtcDateTime,
            Metadata = command.Metadata?.GetRawText()
        };

        lock (Sync)
        {
            Credentials.Add(entity);
        }

        return Task.FromResult<StudentTitleLinked?>(new StudentTitleLinked(
            entity.Id,
            InstitutionId,
            StudentId,
            CareerId,
            WalletId,
            entity.SubjectDid,
            entity.IssuerDid,
            "active",
            now));
    }

    public Task<IReadOnlyList<CredentialSummary>> ListInstitutionCredentialsAsync(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        lock (Sync)
        {
            var items = Credentials
                .Where(c => c.InstitutionId == institutionId)
                .OrderByDescending(c => c.IssuedAt)
                .Select(MapSummary)
                .ToList();

            return Task.FromResult<IReadOnlyList<CredentialSummary>>(items);
        }
    }

    public Task<CredentialSummary?> GetCredentialAsync(Guid credentialId, CancellationToken cancellationToken)
    {
        lock (Sync)
        {
            var entity = Credentials.SingleOrDefault(c => c.Id == credentialId);
            return Task.FromResult(entity is null ? null : MapSummary(entity));
        }
    }

    public Task<CredentialRevoked?> RevokeCredentialAsync(
        RevokeCredentialCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        lock (Sync)
        {
            var entity = Credentials.SingleOrDefault(c => c.Id == command.CredentialId);
            if (entity is null || entity.Status != CredentialStatus.active)
            {
                return Task.FromResult<CredentialRevoked?>(null);
            }

            entity.Status = CredentialStatus.revoked;
            entity.RevokedAt = now.UtcDateTime;
            entity.RevocationReason = command.Reason;
            entity.RevocationTxHash = command.RevocationTxHash;
            entity.RevokedByUserId = command.RevokedByUserId;

            return Task.FromResult<CredentialRevoked?>(new CredentialRevoked(
                entity.Id,
                entity.InstitutionId,
                entity.StudentId,
                "revoked",
                now,
                entity.RevocationReason,
                entity.RevocationTxHash!));
        }
    }

    public Task<string?> GetInstitutionIssuerWalletAsync(Guid institutionId, CancellationToken cancellationToken)
    {
        if (institutionId != InstitutionId)
        {
            return Task.FromResult<string?>(null);
        }

        lock (Sync)
        {
            return Task.FromResult(_issuerWalletAddress);
        }
    }

    public Task<string?> GetInstitutionIssuerWalletForStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken) =>
        studentId == StudentId
            ? GetInstitutionIssuerWalletAsync(InstitutionId, cancellationToken)
            : Task.FromResult<string?>(null);

    private static CredentialSummary MapSummary(CredentialEntity entity) =>
        new(
            entity.Id,
            entity.InstitutionId,
            entity.StudentId,
            entity.CareerId,
            "TITULO",
            entity.SubjectDid,
            entity.IssuerDid,
            entity.Status.ToString(),
            entity.IpfsCid,
            entity.IpfsGatewayUrl,
            entity.ContentHash,
            entity.TransactionHash,
            new DateTimeOffset(DateTime.SpecifyKind(entity.IssuedAt, DateTimeKind.Utc)),
            entity.RevokedAt is null
                ? null
                : new DateTimeOffset(DateTime.SpecifyKind(entity.RevokedAt.Value, DateTimeKind.Utc)),
            entity.RevocationReason,
            "Student Demo");
}
