using System.Text.Json;
using Issuer.Application;

namespace Issuer.Infrastructure.Persistence.Stores;

internal sealed class InMemoryTitleIssuerRepository : ITitleIssuerRepository
{
    public static readonly Guid InstitutionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid StudentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid CareerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid WalletId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly object Sync = new();
    private static readonly List<InMemoryCredential> Credentials = [];
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
            || command.CareerId is null
            || !string.Equals(command.CredentialTypeCode, "TITULO", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<StudentTitleLinked?>(null);
        }

        var entity = new InMemoryCredential
        {
            Id = command.CredentialId ?? Guid.NewGuid(),
            InstitutionId = InstitutionId,
            StudentId = StudentId,
            CareerId = command.CareerId,
            SubjectDid = "did:ethr:sepolia:0x2222222222222222222222222222222222222222",
            IssuerDid = "did:ethr:sepolia:0x1111111111111111111111111111111111111111",
            IpfsCid = command.IpfsCid,
            IpfsGatewayUrl = command.IpfsGatewayUrl,
            ContentHash = command.ContentHash,
            TransactionHash = command.TransactionHash,
            Status = "active",
            IssuedAt = now,
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
            command.CareerId,
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

    public Task<IReadOnlyList<CredentialTypeSummary>> ListCredentialTypesAsync(
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CredentialTypeSummary>>(
        [
            new CredentialTypeSummary(
                1,
                "TITULO",
                "Titulo profesional",
                "Titulo academico o profesional emitido por una institucion.",
                false,
                "1.0")
        ]);

    public Task<CredentialRevoked?> RevokeCredentialAsync(
        RevokeCredentialCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        lock (Sync)
        {
            var entity = Credentials.SingleOrDefault(c => c.Id == command.CredentialId);
            if (entity is null || !string.Equals(entity.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<CredentialRevoked?>(null);
            }

            entity.Status = "revoked";
            entity.RevokedAt = now;
            entity.RevocationReason = command.Reason;
            entity.RevocationTxHash = command.RevocationTxHash;

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

    private static CredentialSummary MapSummary(InMemoryCredential entity) =>
        new(
            entity.Id,
            entity.InstitutionId,
            entity.StudentId,
            entity.CareerId,
            "TITULO",
            entity.SubjectDid,
            entity.IssuerDid,
            entity.Status,
            entity.IpfsCid,
            entity.IpfsGatewayUrl,
            entity.ContentHash,
            entity.TransactionHash,
            entity.IssuedAt,
            entity.RevokedAt,
            entity.RevocationReason,
            "Student Demo");

    private sealed class InMemoryCredential
    {
        public Guid Id { get; init; }
        public Guid InstitutionId { get; init; }
        public Guid StudentId { get; init; }
        public Guid? CareerId { get; init; }
        public string SubjectDid { get; init; } = string.Empty;
        public string IssuerDid { get; init; } = string.Empty;
        public string IpfsCid { get; init; } = string.Empty;
        public string IpfsGatewayUrl { get; init; } = string.Empty;
        public string ContentHash { get; init; } = string.Empty;
        public string TransactionHash { get; init; } = string.Empty;
        public string Status { get; set; } = "active";
        public DateTimeOffset IssuedAt { get; init; }
        public DateTimeOffset? RevokedAt { get; set; }
        public string? RevocationReason { get; set; }
        public string? RevocationTxHash { get; set; }
        public string? Metadata { get; init; }
    }
}
