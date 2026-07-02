using Issuer.Application;
using Issuer.Infrastructure.Persistence.Generated;
using Issuer.Infrastructure.Persistence.Generated.Entities;
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
        institution.PublicKey = command.PublicKey ?? institution.PublicKey;

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

        if (institution?.Did is null or "")
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
                && w.Status == WalletStatus.Active,
                cancellationToken);

        var credentialType = await _dbContext.CredentialTypes
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Code == command.CredentialTypeCode && c.IsActive, cancellationToken);

        if (wallet is null || credentialType is null)
        {
            return null;
        }

        var entity = new Credential
        {
            Id = Guid.NewGuid(),
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
            Status = CredentialStatus.Active,
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
            "active",
            ToDateTimeOffset(entity.IssuedAt));
    }

    private static DateTime UtcDateTime(DateTimeOffset value) => value.UtcDateTime;

    private static DateTimeOffset ToDateTimeOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
