using Academy.Application;
using Academy.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Academy.Infrastructure.Persistence.Stores;

internal sealed class PostgresAcademyRepository : IAcademyRepository
{
    private readonly AcademyDbContext _dbContext;

    public PostgresAcademyRepository(AcademyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> InstitutionCodeExistsAsync(string code, CancellationToken cancellationToken) =>
        _dbContext.Institutions
            .AsNoTracking()
            .AnyAsync(i => i.Code == code, cancellationToken);

    public async Task<InstitutionSummary?> GetInstitutionAsync(Guid institutionId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Institutions
            .AsNoTracking()
            .SingleOrDefaultAsync(i => i.Id == institutionId, cancellationToken);

        return entity is null ? null : ToSummary(entity);
    }

    public async Task<InstitutionSummary> CreateInstitutionAsync(
        CreateInstitutionCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var entity = new InstitutionEntity
        {
            Id = Guid.NewGuid(),
            Code = command.Code,
            LegalName = command.LegalName,
            DisplayName = command.DisplayName,
            CountryCode = command.CountryCode,
            WebsiteUrl = command.WebsiteUrl,
            IsActive = true,
            RegisteredAt = UtcDateTime(now)
        };

        _dbContext.Institutions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToSummary(entity);
    }

    public Task<bool> CareerCodeExistsAsync(Guid institutionId, string code, CancellationToken cancellationToken) =>
        _dbContext.Careers
            .AsNoTracking()
            .AnyAsync(c => c.InstitutionId == institutionId && c.Code == code, cancellationToken);

    public async Task<CareerSummary> CreateCareerAsync(
        CreateCareerCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var entity = new CareerEntity
        {
            Id = Guid.NewGuid(),
            InstitutionId = command.InstitutionId,
            Code = command.Code,
            Name = command.Name,
            IsActive = true,
            CreatedAt = UtcDateTime(now)
        };

        _dbContext.Careers.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToSummary(entity);
    }

    public Task<bool> StudentExternalReferenceExistsAsync(
        Guid institutionId,
        string externalReference,
        CancellationToken cancellationToken) =>
        _dbContext.Students
            .AsNoTracking()
            .AnyAsync(s => s.InstitutionId == institutionId && s.ExternalReference == externalReference, cancellationToken);

    public async Task<StudentSummary> CreateStudentAsync(
        CreateStudentCommand command,
        string? walletDid,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var student = new StudentEntity
        {
            Id = Guid.NewGuid(),
            InstitutionId = command.InstitutionId,
            ExternalReference = command.ExternalReference,
            EnrollmentYear = command.EnrollmentYear,
            IsActive = true,
            CreatedAt = UtcDateTime(now)
        };

        _dbContext.Students.Add(student);
        await _dbContext.SaveChangesAsync(cancellationToken);

        StudentWalletEntity? wallet = null;
        if (command.WalletAddress is not null && walletDid is not null)
        {
            wallet = new StudentWalletEntity
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                WalletAddress = command.WalletAddress,
                Did = walletDid,
                Status = WalletStatus.active,
                IsPrimary = true,
                ActivatedAt = UtcDateTime(now)
            };
            _dbContext.StudentWallets.Add(wallet);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new StudentSummary(
            student.Id,
            student.InstitutionId,
            student.ExternalReference,
            student.EnrollmentYear,
            wallet?.Id,
            wallet?.WalletAddress,
            wallet?.Did,
            student.IsActive,
            ToDateTimeOffset(student.CreatedAt));
    }

    public async Task<InstitutionInvitationCreated> CreateInvitationAsync(
        CreateInstitutionInvitationCommand command,
        string tokenHash,
        string invitationUrl,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var entity = new InstitutionInvitationEntity
        {
            Id = Guid.NewGuid(),
            InstitutionId = command.InstitutionId,
            Email = command.Email,
            Role = ParseRole(command.Role),
            TokenHash = tokenHash,
            InvitationUrl = invitationUrl,
            ExpiresAt = UtcDateTime(expiresAt),
            CreatedAt = UtcDateTime(now),
            CreatedByUserId = command.CreatedByUserId
        };

        _dbContext.InstitutionInvitations.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToInvitation(entity);
    }

    public async Task<InstitutionInvitationAccepted?> AcceptInvitationAsync(
        string tokenHash,
        string walletAddress,
        string did,
        string? displayName,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var nowDateTime = UtcDateTime(now);
        var invitation = await _dbContext.InstitutionInvitations
            .SingleOrDefaultAsync(i =>
                i.TokenHash == tokenHash
                && i.AcceptedAt == null
                && i.RevokedAt == null
                && i.ExpiresAt > nowDateTime,
                cancellationToken);

        if (invitation is null)
        {
            return null;
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.WalletAddress == walletAddress, cancellationToken);
        if (user is null)
        {
            user = new UserEntity
            {
                Id = Guid.NewGuid(),
                WalletAddress = walletAddress,
                Did = did,
                Email = invitation.Email,
                DisplayName = displayName,
                IsActive = true,
                CreatedAt = nowDateTime
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            user.Email ??= invitation.Email;
            user.DisplayName ??= displayName;
            user.IsActive = true;
        }

        var roleExists = await _dbContext.InstitutionUsers.AnyAsync(iu =>
            iu.InstitutionId == invitation.InstitutionId
            && iu.UserId == user.Id
            && iu.Role == invitation.Role
            && iu.RevokedAt == null,
            cancellationToken);

        if (!roleExists)
        {
            _dbContext.InstitutionUsers.Add(new InstitutionUserEntity
            {
                Id = Guid.NewGuid(),
                InstitutionId = invitation.InstitutionId,
                UserId = user.Id,
                Role = invitation.Role,
                GrantedAt = nowDateTime
            });
        }

        invitation.AcceptedAt = nowDateTime;
        invitation.AcceptedByUserId = user.Id;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new InstitutionInvitationAccepted(
            invitation.InstitutionId,
            user.Id,
            walletAddress,
            did,
            invitation.Role.ToString());
    }

    private static InstitutionSummary ToSummary(InstitutionEntity entity) =>
        new(
            entity.Id,
            entity.Code,
            entity.LegalName,
            entity.DisplayName,
            entity.Did,
            entity.IssuerWalletAddress,
            entity.CountryCode,
            entity.WebsiteUrl,
            entity.IsActive,
            ToDateTimeOffset(entity.RegisteredAt));

    private static CareerSummary ToSummary(CareerEntity entity) =>
        new(
            entity.Id,
            entity.InstitutionId,
            entity.Code,
            entity.Name,
            entity.IsActive,
            ToDateTimeOffset(entity.CreatedAt));

    private static InstitutionInvitationCreated ToInvitation(InstitutionInvitationEntity entity) =>
        new(
            entity.Id,
            entity.InstitutionId,
            entity.Email,
            entity.Role.ToString(),
            entity.InvitationUrl,
            ToDateTimeOffset(entity.ExpiresAt));

    private static UserRole ParseRole(string role) => Enum.Parse<UserRole>(role, ignoreCase: true);

    private static DateTime UtcDateTime(DateTimeOffset value) => value.UtcDateTime;

    private static DateTimeOffset ToDateTimeOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}

