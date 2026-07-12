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

    public async Task<IReadOnlyList<InstitutionSummary>> ListInstitutionsAsync(CancellationToken cancellationToken)
    {
        var entities = await _dbContext.Institutions
            .AsNoTracking()
            .OrderBy(i => i.Code)
            .ToListAsync(cancellationToken);

        var fallbackWallets = await LoadIssuerWalletFallbacksAsync(
            entities
                .Where(i => string.IsNullOrEmpty(i.IssuerWalletAddress))
                .Select(i => i.Id)
                .ToList(),
            cancellationToken);

        return entities
            .Select(entity => ToSummary(entity, fallbackWallets.GetValueOrDefault(entity.Id)))
            .ToList();
    }

    public async Task<InstitutionSummary?> GetInstitutionAsync(Guid institutionId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Institutions
            .AsNoTracking()
            .SingleOrDefaultAsync(i => i.Id == institutionId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var fallbackWallet = string.IsNullOrEmpty(entity.IssuerWalletAddress)
            ? await FindIssuerWalletFallbackAsync(entity.Id, cancellationToken)
            : null;

        return ToSummary(entity, fallbackWallet);
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

    public async Task<InstitutionSummary?> UpdateInstitutionAsync(
        UpdateInstitutionCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Institutions
            .SingleOrDefaultAsync(i => i.Id == command.InstitutionId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.LegalName = command.LegalName;
        entity.DisplayName = command.DisplayName;
        entity.CountryCode = command.CountryCode;
        entity.WebsiteUrl = command.WebsiteUrl;
        entity.IsActive = command.IsActive;
        entity.DeactivatedAt = command.IsActive ? null : UtcDateTime(now);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToSummary(entity);
    }

    public Task<bool> CareerCodeExistsAsync(Guid institutionId, string code, CancellationToken cancellationToken) =>
        _dbContext.Careers
            .AsNoTracking()
            .AnyAsync(c => c.InstitutionId == institutionId && c.Code == code, cancellationToken);

    public Task<bool> CareerCodeExistsAsync(
        Guid institutionId,
        string code,
        Guid excludingCareerId,
        CancellationToken cancellationToken) =>
        _dbContext.Careers
            .AsNoTracking()
            .AnyAsync(
                c => c.InstitutionId == institutionId
                    && c.Id != excludingCareerId
                    && c.Code == code,
                cancellationToken);

    public async Task<IReadOnlyList<CareerSummary>> ListCareersAsync(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        var entities = await _dbContext.Careers
            .AsNoTracking()
            .Where(c => c.InstitutionId == institutionId)
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToSummary).ToList();
    }

    public async Task<CareerSummary?> GetCareerAsync(
        Guid institutionId,
        Guid careerId,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Careers
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == careerId && c.InstitutionId == institutionId, cancellationToken);

        return entity is null ? null : ToSummary(entity);
    }

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

    public async Task<CareerSummary?> UpdateCareerAsync(
        UpdateCareerCommand command,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Careers
            .SingleOrDefaultAsync(
                c => c.Id == command.CareerId && c.InstitutionId == command.InstitutionId,
                cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Code = command.Code;
        entity.Name = command.Name;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToSummary(entity);
    }

    public async Task<CareerSummary?> DeactivateCareerAsync(
        Guid institutionId,
        Guid careerId,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Careers
            .SingleOrDefaultAsync(c => c.Id == careerId && c.InstitutionId == institutionId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.IsActive = false;
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

    public async Task<IReadOnlyList<StudentSummary>> ListStudentsAsync(Guid institutionId, CancellationToken cancellationToken)
    {
        var rows = await (
            from student in _dbContext.Students.AsNoTracking()
            where student.InstitutionId == institutionId
            join wallet in _dbContext.StudentWallets.AsNoTracking().Where(wallet =>
                    wallet.IsPrimary && wallet.Status == WalletStatus.active)
                on student.Id equals wallet.StudentId into wallets
            from primaryWallet in wallets.DefaultIfEmpty()
            orderby student.CreatedAt
            select new { student, primaryWallet })
            .ToListAsync(cancellationToken);

        return rows.Select(row => ToStudentSummary(row.student, row.primaryWallet)).ToList();
    }

    public async Task<StudentSummary?> GetStudentAsync(
        Guid institutionId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var row = await (
            from student in _dbContext.Students.AsNoTracking()
            where student.Id == studentId && student.InstitutionId == institutionId
            join wallet in _dbContext.StudentWallets.AsNoTracking().Where(wallet =>
                    wallet.IsPrimary && wallet.Status == WalletStatus.active)
                on student.Id equals wallet.StudentId into wallets
            from primaryWallet in wallets.DefaultIfEmpty()
            select new { student, primaryWallet })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null ? null : ToStudentSummary(row.student, row.primaryWallet);
    }

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

        return ToStudentSummary(student, wallet);
    }

    public async Task<StudentWalletSummary?> AddStudentWalletAsync(
        AddStudentWalletCommand command,
        string did,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var studentExists = await _dbContext.Students
            .AsNoTracking()
            .AnyAsync(s =>
                s.Id == command.StudentId
                && s.InstitutionId == command.InstitutionId
                && s.IsActive,
                cancellationToken);

        if (!studentExists)
        {
            return null;
        }

        if (command.MakePrimary)
        {
            await _dbContext.StudentWallets
                .Where(wallet =>
                    wallet.StudentId == command.StudentId
                    && wallet.IsPrimary
                    && wallet.Status == WalletStatus.active)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(wallet => wallet.IsPrimary, false),
                    cancellationToken);
        }

        var wallet = new StudentWalletEntity
        {
            Id = Guid.NewGuid(),
            StudentId = command.StudentId,
            WalletAddress = command.WalletAddress,
            Did = did,
            Status = WalletStatus.active,
            IsPrimary = command.MakePrimary,
            ActivatedAt = UtcDateTime(now)
        };

        _dbContext.StudentWallets.Add(wallet);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToSummary(wallet);
    }

    public async Task<HolderDashboard> GetHolderDashboardAsync(
        string walletAddress,
        string did,
        CancellationToken cancellationToken)
    {
        var userRow = await (
            from user in _dbContext.Users.AsNoTracking()
            where user.WalletAddress == walletAddress
            join holderProfileEntity in _dbContext.HolderProfiles.AsNoTracking()
                on user.Id equals holderProfileEntity.UserId into profiles
            from holderProfile in profiles.DefaultIfEmpty()
            select new { user, holderProfile })
            .SingleOrDefaultAsync(cancellationToken);

        var profile = userRow is null
            ? new HolderProfile(walletAddress, did, null, null, null, null, null, null, null)
            : ToHolderProfile(userRow.user, userRow.holderProfile);

        var institutionRows = await (
            from wallet in _dbContext.StudentWallets.AsNoTracking()
            where wallet.WalletAddress == walletAddress && wallet.Status == WalletStatus.active
            join student in _dbContext.Students.AsNoTracking()
                on wallet.StudentId equals student.Id
            join institution in _dbContext.Institutions.AsNoTracking()
                on student.InstitutionId equals institution.Id
            where student.IsActive && institution.IsActive
            orderby institution.DisplayName, student.CreatedAt
            select new { institution, student, wallet })
            .ToListAsync(cancellationToken);

        var institutions = institutionRows
            .Select(row => new HolderInstitutionSummary(
                row.institution.Id,
                row.institution.Code,
                row.institution.DisplayName,
                row.student.Id,
                row.student.ExternalReference,
                row.student.EnrollmentYear,
                row.wallet.WalletAddress,
                row.wallet.Did,
                row.wallet.IsPrimary,
                ToDateTimeOffset(row.wallet.ActivatedAt)))
            .ToList();

        return new HolderDashboard(profile, institutions);
    }

    public async Task<HolderProfile> UpdateHolderProfileAsync(
        UpdateHolderProfileCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var nowDateTime = UtcDateTime(now);
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(u => u.WalletAddress == command.WalletAddress, cancellationToken);

        if (user is null)
        {
            user = new UserEntity
            {
                Id = Guid.NewGuid(),
                WalletAddress = command.WalletAddress,
                Did = command.Did,
                DisplayName = command.DisplayName,
                IsActive = true,
                CreatedAt = nowDateTime
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            user.Did = command.Did;
            user.DisplayName = command.DisplayName;
            user.IsActive = true;
        }

        var profile = await _dbContext.HolderProfiles
            .SingleOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

        if (profile is null)
        {
            profile = new HolderProfileEntity
            {
                UserId = user.Id,
                CreatedAt = nowDateTime
            };
            _dbContext.HolderProfiles.Add(profile);
        }

        profile.FullName = command.FullName;
        profile.BirthDate = command.BirthDate;
        profile.ContactEmail = command.ContactEmail;
        profile.CountryCode = command.CountryCode;
        profile.PhoneNumber = command.PhoneNumber;
        profile.UpdatedAt = nowDateTime;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToHolderProfile(user, profile);
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

    public async Task<InvitationAcceptResult> AcceptInvitationAsync(
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
            return new InvitationAcceptResult(null, null);
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.WalletAddress == walletAddress, cancellationToken);
        if (user is null)
        {
            var userByEmail = await _dbContext.Users
                .SingleOrDefaultAsync(
                    u => u.Email != null && u.Email.ToLower() == invitation.Email.ToLower(),
                    cancellationToken);

            if (userByEmail is not null)
            {
                if (!string.Equals(userByEmail.WalletAddress, walletAddress, StringComparison.OrdinalIgnoreCase))
                {
                    return new InvitationAcceptResult(null, "invitation_wallet_email_mismatch");
                }

                user = userByEmail;
            }
            else
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

        if (invitation.Role is UserRole.admin or UserRole.issuer)
        {
            var institution = await _dbContext.Institutions
                .SingleAsync(i => i.Id == invitation.InstitutionId, cancellationToken);

            if (string.IsNullOrEmpty(institution.IssuerWalletAddress))
            {
                institution.IssuerWalletAddress = walletAddress;
                institution.Did ??= did;
            }
        }

        invitation.AcceptedAt = nowDateTime;
        invitation.AcceptedByUserId = user.Id;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new InvitationAcceptResult(
            new InstitutionInvitationAccepted(
                invitation.InstitutionId,
                user.Id,
                walletAddress,
                did,
                invitation.Role.ToString()),
            null);
    }

    public async Task<IReadOnlyList<InstitutionUserSummary>> ListInstitutionUsersAsync(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from institutionUser in _dbContext.InstitutionUsers.AsNoTracking()
            join user in _dbContext.Users.AsNoTracking()
                on institutionUser.UserId equals user.Id
            where institutionUser.InstitutionId == institutionId
                && institutionUser.RevokedAt == null
            orderby user.Email, user.WalletAddress
            select new { institutionUser, user })
            .ToListAsync(cancellationToken);

        return rows.Select(row => ToSummary(row.institutionUser, row.user)).ToList();
    }

    public async Task<InstitutionUserSummary?> UpdateInstitutionUserRoleAsync(
        Guid institutionId,
        Guid userId,
        string role,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var parsedRole = ParseRole(role);
        var institutionUser = await _dbContext.InstitutionUsers
            .SingleOrDefaultAsync(iu =>
                iu.InstitutionId == institutionId
                && iu.UserId == userId
                && iu.RevokedAt == null,
                cancellationToken);

        if (institutionUser is null)
        {
            return null;
        }

        institutionUser.Role = parsedRole;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleAsync(u => u.Id == userId, cancellationToken);

        return ToSummary(institutionUser, user);
    }

    public async Task<bool> RevokeInstitutionUserAsync(
        Guid institutionId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var affected = await _dbContext.InstitutionUsers
            .Where(iu =>
                iu.InstitutionId == institutionId
                && iu.UserId == userId
                && iu.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(iu => iu.RevokedAt, UtcDateTime(now)),
                cancellationToken);

        return affected > 0;
    }

    private static InstitutionSummary ToSummary(
        InstitutionEntity entity,
        IssuerWalletFallback? issuerWalletFallback = null) =>
        new(
            entity.Id,
            entity.Code,
            entity.LegalName,
            entity.DisplayName,
            issuerWalletFallback?.Did ?? entity.Did,
            issuerWalletFallback?.WalletAddress ?? entity.IssuerWalletAddress,
            entity.CountryCode,
            entity.WebsiteUrl,
            entity.IsActive,
            ToDateTimeOffset(entity.RegisteredAt));

    private sealed record IssuerWalletFallback(string WalletAddress, string? Did);

    private async Task<Dictionary<Guid, IssuerWalletFallback>> LoadIssuerWalletFallbacksAsync(
        IReadOnlyList<Guid> institutionIds,
        CancellationToken cancellationToken)
    {
        if (institutionIds.Count == 0)
        {
            return [];
        }

        var candidates = await (
                from institutionUser in _dbContext.InstitutionUsers.AsNoTracking()
                join user in _dbContext.Users.AsNoTracking() on institutionUser.UserId equals user.Id
                where institutionIds.Contains(institutionUser.InstitutionId)
                      && institutionUser.RevokedAt == null
                      && (institutionUser.Role == UserRole.admin || institutionUser.Role == UserRole.issuer)
                orderby institutionUser.GrantedAt
                select new
                {
                    institutionUser.InstitutionId,
                    user.WalletAddress,
                    user.Did,
                    institutionUser.GrantedAt
                })
            .ToListAsync(cancellationToken);

        return candidates
            .GroupBy(candidate => candidate.InstitutionId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var first = group.OrderBy(candidate => candidate.GrantedAt).First();
                    return new IssuerWalletFallback(first.WalletAddress, first.Did);
                });
    }

    private async Task<IssuerWalletFallback?> FindIssuerWalletFallbackAsync(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        var fallback = await (
                from institutionUser in _dbContext.InstitutionUsers.AsNoTracking()
                join user in _dbContext.Users.AsNoTracking() on institutionUser.UserId equals user.Id
                where institutionUser.InstitutionId == institutionId
                      && institutionUser.RevokedAt == null
                      && (institutionUser.Role == UserRole.admin || institutionUser.Role == UserRole.issuer)
                orderby institutionUser.GrantedAt
                select new IssuerWalletFallback(user.WalletAddress, user.Did))
            .FirstOrDefaultAsync(cancellationToken);

        return fallback;
    }

    private static CareerSummary ToSummary(CareerEntity entity) =>
        new(
            entity.Id,
            entity.InstitutionId,
            entity.Code,
            entity.Name,
            entity.IsActive,
            ToDateTimeOffset(entity.CreatedAt));

    private static StudentSummary ToStudentSummary(StudentEntity student, StudentWalletEntity? wallet) =>
        new(
            student.Id,
            student.InstitutionId,
            student.ExternalReference,
            student.EnrollmentYear,
            wallet?.Id,
            wallet?.WalletAddress,
            wallet?.Did,
            student.IsActive,
            ToDateTimeOffset(student.CreatedAt));

    private static StudentWalletSummary ToSummary(StudentWalletEntity wallet) =>
        new(
            wallet.Id,
            wallet.StudentId,
            wallet.WalletAddress,
            wallet.Did,
            wallet.Status.ToString(),
            wallet.IsPrimary,
            ToDateTimeOffset(wallet.ActivatedAt));

    private static InstitutionUserSummary ToSummary(InstitutionUserEntity institutionUser, UserEntity user) =>
        new(
            institutionUser.Id,
            institutionUser.InstitutionId,
            institutionUser.UserId,
            user.WalletAddress,
            user.Did,
            user.Email,
            user.DisplayName,
            institutionUser.Role.ToString(),
            ToDateTimeOffset(institutionUser.GrantedAt),
            institutionUser.RevokedAt is null ? null : ToDateTimeOffset(institutionUser.RevokedAt.Value));

    private static HolderProfile ToHolderProfile(UserEntity user, HolderProfileEntity? profile) =>
        new(
            user.WalletAddress,
            user.Did,
            user.DisplayName,
            profile?.FullName,
            profile?.BirthDate,
            profile?.ContactEmail,
            profile?.CountryCode,
            profile?.PhoneNumber,
            profile is null ? null : ToDateTimeOffset(profile.UpdatedAt));

    private static InstitutionInvitationCreated ToInvitation(InstitutionInvitationEntity entity) =>
        new(
            entity.Id,
            entity.InstitutionId,
            entity.Email,
            entity.Role.ToString(),
            entity.InvitationUrl,
            ToDateTimeOffset(entity.ExpiresAt));

    private static UserRole ParseRole(string role) => Enum.Parse<UserRole>(role, ignoreCase: true);

    private static DateTime UtcDateTime(DateTimeOffset value) =>
        DateTime.SpecifyKind(value.UtcDateTime, DateTimeKind.Unspecified);

    private static DateTimeOffset ToDateTimeOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}

