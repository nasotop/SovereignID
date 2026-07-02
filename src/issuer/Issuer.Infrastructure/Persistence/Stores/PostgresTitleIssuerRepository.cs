using Issuer.Application;
using Issuer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Issuer.Infrastructure.Persistence.Stores;

internal sealed class PostgresTitleIssuerRepository : ITitleIssuerRepository
{
    private readonly IssuerDbContext _dbContext;

    public PostgresTitleIssuerRepository(IssuerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InstitutionIssuerWalletLinked?> LinkInstitutionIssuerWalletAsync(
        LinkInstitutionIssuerWalletCommand command,
        CancellationToken cancellationToken)
    {
        var institution = await _dbContext.Institutions
            .SingleOrDefaultAsync(i => i.Id == command.InstitutionId && i.IsActive, cancellationToken);

        if (institution is null)
        {
            return null;
        }

        institution.IssuerWalletAddress = command.WalletAddress;
        institution.Did = command.Did;
        institution.PublicKey = command.PublicKey;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new InstitutionIssuerWalletLinked(
            institution.Id,
            institution.IssuerWalletAddress,
            institution.Did,
            institution.PublicKey);
    }

    public async Task<StudentTitleLinked?> LinkStudentTitleAsync(
        LinkStudentTitleCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var student = await _dbContext.Students
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == command.StudentId && s.IsActive, cancellationToken);

        if (student is null)
        {
            return null;
        }

        var institution = await _dbContext.Institutions
            .AsNoTracking()
            .SingleOrDefaultAsync(i => i.Id == student.InstitutionId && i.IsActive, cancellationToken);

        if (institution?.Did is null)
        {
            return null;
        }

        if (command.CareerId is not null)
        {
            var careerExists = await _dbContext.Careers
                .AsNoTracking()
                .AnyAsync(c => c.Id == command.CareerId && c.InstitutionId == student.InstitutionId && c.IsActive, cancellationToken);
            if (!careerExists)
            {
                return null;
            }
        }

        var wallet = await _dbContext.StudentWallets
            .AsNoTracking()
            .SingleOrDefaultAsync(w =>
                w.StudentId == student.Id
                && w.IsPrimary
                && w.Status == WalletStatus.active,
                cancellationToken);

        var credentialType = await _dbContext.CredentialTypes
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Code == command.CredentialTypeCode && c.IsActive, cancellationToken);

        if (wallet is null || credentialType is null)
        {
            return null;
        }

        var entity = new CredentialEntity
        {
            Id = command.CredentialId ?? Guid.NewGuid(),
            InstitutionId = student.InstitutionId,
            CredentialTypeId = credentialType.Id,
            StudentId = student.Id,
            CareerId = command.CareerId,
            IssuedToWalletId = wallet.Id,
            SubjectDid = wallet.Did,
            IssuerDid = institution.Did,
            IpfsCid = command.IpfsCid,
            IpfsGatewayUrl = command.IpfsGatewayUrl,
            ContentHash = command.ContentHash,
            TransactionHash = command.TransactionHash,
            BlockNumber = command.BlockNumber,
            ChainId = command.ChainId!.Value,
            Eip712Signature = command.Eip712Signature,
            Status = CredentialStatus.active,
            IssuedAt = UtcDateTime(now),
            ExpiresAt = command.ExpiresAt is null ? null : UtcDateTime(command.ExpiresAt.Value),
            CreatedAt = UtcDateTime(now),
            Metadata = command.Metadata?.GetRawText()
        };

        _dbContext.Credentials.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new StudentTitleLinked(
            entity.Id,
            entity.InstitutionId,
            entity.StudentId,
            entity.CareerId,
            entity.IssuedToWalletId,
            entity.SubjectDid,
            entity.IssuerDid,
            entity.Status.ToString(),
            ToDateTimeOffset(entity.IssuedAt));
    }

    public async Task<IReadOnlyList<CredentialSummary>> ListInstitutionCredentialsAsync(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        var credentials = await (
            from credential in _dbContext.Credentials.AsNoTracking()
            join type in _dbContext.CredentialTypes.AsNoTracking() on credential.CredentialTypeId equals type.Id
            join student in _dbContext.Students.AsNoTracking() on credential.StudentId equals student.Id
            where credential.InstitutionId == institutionId
            orderby credential.IssuedAt descending
            select new { credential, type, student }
        ).ToListAsync(cancellationToken);

        return credentials
            .Select(item => MapSummary(item.credential, item.type.Code, item.student.ExternalReference))
            .ToList();
    }

    public async Task<CredentialSummary?> GetCredentialAsync(Guid credentialId, CancellationToken cancellationToken)
    {
        var item = await (
            from credential in _dbContext.Credentials.AsNoTracking()
            join type in _dbContext.CredentialTypes.AsNoTracking() on credential.CredentialTypeId equals type.Id
            join student in _dbContext.Students.AsNoTracking() on credential.StudentId equals student.Id
            where credential.Id == credentialId
            select new { credential, type, student }
        ).SingleOrDefaultAsync(cancellationToken);

        return item is null
            ? null
            : MapSummary(item.credential, item.type.Code, item.student.ExternalReference);
    }

    public async Task<CredentialRevoked?> RevokeCredentialAsync(
        RevokeCredentialCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Credentials
            .SingleOrDefaultAsync(c => c.Id == command.CredentialId, cancellationToken);

        if (entity is null || entity.Status != CredentialStatus.active)
        {
            return null;
        }

        entity.Status = CredentialStatus.revoked;
        entity.RevokedAt = UtcDateTime(now);
        entity.RevocationReason = command.Reason;
        entity.RevocationTxHash = command.RevocationTxHash;
        entity.RevokedByUserId = command.RevokedByUserId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CredentialRevoked(
            entity.Id,
            entity.InstitutionId,
            entity.StudentId,
            entity.Status.ToString(),
            now,
            entity.RevocationReason,
            entity.RevocationTxHash!);
    }

    public async Task<string?> GetInstitutionIssuerWalletAsync(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        var institution = await _dbContext.Institutions
            .AsNoTracking()
            .SingleOrDefaultAsync(i => i.Id == institutionId && i.IsActive, cancellationToken);

        return institution?.IssuerWalletAddress;
    }

    public async Task<string?> GetInstitutionIssuerWalletForStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var student = await _dbContext.Students
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == studentId && s.IsActive, cancellationToken);

        if (student is null)
        {
            return null;
        }

        return await GetInstitutionIssuerWalletAsync(student.InstitutionId, cancellationToken);
    }

    private static CredentialSummary MapSummary(
        CredentialEntity entity,
        string credentialTypeCode,
        string? studentExternalReference) =>
        new(
            entity.Id,
            entity.InstitutionId,
            entity.StudentId,
            entity.CareerId,
            credentialTypeCode,
            entity.SubjectDid,
            entity.IssuerDid,
            entity.Status.ToString(),
            entity.IpfsCid,
            entity.IpfsGatewayUrl,
            entity.ContentHash,
            entity.TransactionHash,
            ToDateTimeOffset(entity.IssuedAt),
            entity.RevokedAt is null ? null : ToDateTimeOffset(entity.RevokedAt.Value),
            entity.RevocationReason,
            studentExternalReference);

    private static DateTime UtcDateTime(DateTimeOffset value) => value.UtcDateTime;

    private static DateTimeOffset ToDateTimeOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
