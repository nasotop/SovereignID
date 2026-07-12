using Academy.Application;

namespace Academy.Infrastructure.Persistence.Stores;

internal sealed class InMemoryAcademyRepository : IAcademyRepository
{
    private readonly Lock _lock = new();
    private readonly Dictionary<Guid, InstitutionSummary> _institutions = [];
    private readonly Dictionary<Guid, CareerSummary> _careers = [];
    private readonly Dictionary<Guid, StudentSummary> _students = [];
    private readonly Dictionary<Guid, StudentWalletSummary> _studentWallets = [];
    private readonly Dictionary<Guid, InvitationState> _invitations = [];
    private readonly Dictionary<string, Guid> _usersByWallet = [];
    private readonly Dictionary<Guid, UserState> _users = [];
    private readonly Dictionary<Guid, HolderProfileState> _holderProfiles = [];
    private readonly Dictionary<Guid, InstitutionUserSummary> _institutionUsers = [];

    public Task<bool> InstitutionCodeExistsAsync(string code, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(_institutions.Values.Any(i => string.Equals(i.Code, code, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<IReadOnlyList<InstitutionSummary>> ListInstitutionsAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<InstitutionSummary>>(
                _institutions.Values
                    .OrderBy(i => i.Code, StringComparer.OrdinalIgnoreCase)
                    .Select(EnrichInstitutionSummary)
                    .ToList());
        }
    }

    public Task<InstitutionSummary?> GetInstitutionAsync(Guid institutionId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!_institutions.TryGetValue(institutionId, out var institution))
            {
                return Task.FromResult<InstitutionSummary?>(null);
            }

            return Task.FromResult<InstitutionSummary?>(EnrichInstitutionSummary(institution));
        }
    }

    public Task<InstitutionSummary> CreateInstitutionAsync(
        CreateInstitutionCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var institution = new InstitutionSummary(
                Guid.NewGuid(),
                command.Code,
                command.LegalName,
                command.DisplayName,
                null,
                null,
                command.CountryCode,
                command.WebsiteUrl,
                true,
                now);

            _institutions.Add(institution.Id, institution);
            return Task.FromResult(institution);
        }
    }

    public Task<InstitutionSummary?> UpdateInstitutionAsync(
        UpdateInstitutionCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!_institutions.TryGetValue(command.InstitutionId, out var institution))
            {
                return Task.FromResult<InstitutionSummary?>(null);
            }

            var updated = institution with
            {
                LegalName = command.LegalName,
                DisplayName = command.DisplayName,
                CountryCode = command.CountryCode,
                WebsiteUrl = command.WebsiteUrl,
                IsActive = command.IsActive
            };

            _institutions[command.InstitutionId] = updated;
            return Task.FromResult<InstitutionSummary?>(EnrichInstitutionSummary(updated));
        }
    }

    public Task<bool> CareerCodeExistsAsync(Guid institutionId, string code, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(_careers.Values.Any(c => c.InstitutionId == institutionId && string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<bool> CareerCodeExistsAsync(
        Guid institutionId,
        string code,
        Guid excludingCareerId,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(_careers.Values.Any(c =>
                c.InstitutionId == institutionId
                && c.Id != excludingCareerId
                && string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<IReadOnlyList<CareerSummary>> ListCareersAsync(Guid institutionId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<CareerSummary>>(
                _careers.Values
                    .Where(c => c.InstitutionId == institutionId)
                    .OrderByDescending(c => c.IsActive)
                    .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList());
        }
    }

    public Task<CareerSummary?> GetCareerAsync(Guid institutionId, Guid careerId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(
                _careers.TryGetValue(careerId, out var career) && career.InstitutionId == institutionId
                    ? career
                    : null);
        }
    }

    public Task<CareerSummary> CreateCareerAsync(CreateCareerCommand command, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var career = new CareerSummary(Guid.NewGuid(), command.InstitutionId, command.Code, command.Name, true, now);
            _careers.Add(career.Id, career);
            return Task.FromResult(career);
        }
    }

    public Task<CareerSummary?> UpdateCareerAsync(UpdateCareerCommand command, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!_careers.TryGetValue(command.CareerId, out var career) || career.InstitutionId != command.InstitutionId)
            {
                return Task.FromResult<CareerSummary?>(null);
            }

            var updated = career with
            {
                Code = command.Code,
                Name = command.Name
            };
            _careers[career.Id] = updated;
            return Task.FromResult<CareerSummary?>(updated);
        }
    }

    public Task<CareerSummary?> DeactivateCareerAsync(Guid institutionId, Guid careerId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!_careers.TryGetValue(careerId, out var career) || career.InstitutionId != institutionId)
            {
                return Task.FromResult<CareerSummary?>(null);
            }

            var updated = career with { IsActive = false };
            _careers[career.Id] = updated;
            return Task.FromResult<CareerSummary?>(updated);
        }
    }

    public Task<bool> StudentExternalReferenceExistsAsync(Guid institutionId, string externalReference, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(_students.Values.Any(s => s.InstitutionId == institutionId && string.Equals(s.ExternalReference, externalReference, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<StudentSummary> CreateStudentAsync(
        CreateStudentCommand command,
        string? walletDid,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            Guid? walletId = command.WalletAddress is null ? null : Guid.NewGuid();
            if (walletId is Guid newWalletId && walletDid is not null && command.WalletAddress is not null)
            {
                _studentWallets[newWalletId] = new StudentWalletSummary(
                    newWalletId,
                    Guid.Empty,
                    command.WalletAddress,
                    walletDid,
                    "active",
                    true,
                    now);
            }

            var student = new StudentSummary(
                Guid.NewGuid(),
                command.InstitutionId,
                command.ExternalReference,
                command.EnrollmentYear,
                walletId,
                command.WalletAddress,
                walletDid,
                true,
                now);

            _students.Add(student.Id, student);
            if (walletId is Guid createdWalletId && _studentWallets.TryGetValue(createdWalletId, out var wallet))
            {
                _studentWallets[createdWalletId] = wallet with { StudentId = student.Id };
            }

            return Task.FromResult(student);
        }
    }

    public Task<IReadOnlyList<StudentSummary>> ListStudentsAsync(Guid institutionId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<StudentSummary>>(
                _students.Values
                    .Where(s => s.InstitutionId == institutionId)
                    .OrderBy(s => s.CreatedAt)
                    .ToList());
        }
    }

    public Task<StudentSummary?> GetStudentAsync(Guid institutionId, Guid studentId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(
                _students.TryGetValue(studentId, out var student) && student.InstitutionId == institutionId
                    ? student
                    : null);
        }
    }

    public Task<StudentWalletSummary?> AddStudentWalletAsync(
        AddStudentWalletCommand command,
        string did,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!_students.TryGetValue(command.StudentId, out var student) || student.InstitutionId != command.InstitutionId)
            {
                return Task.FromResult<StudentWalletSummary?>(null);
            }

            if (command.MakePrimary)
            {
                foreach (var current in _studentWallets.Values.Where(w => w.StudentId == student.Id && w.IsPrimary).ToList())
                {
                    _studentWallets[current.Id] = current with { IsPrimary = false };
                }
            }

            var wallet = new StudentWalletSummary(
                Guid.NewGuid(),
                student.Id,
                command.WalletAddress,
                did,
                "active",
                command.MakePrimary,
                now);

            _studentWallets[wallet.Id] = wallet;
            if (command.MakePrimary)
            {
                _students[student.Id] = student with
                {
                    PrimaryWalletId = wallet.Id,
                    PrimaryWalletAddress = wallet.WalletAddress,
                    PrimaryWalletDid = wallet.Did
                };
            }

            return Task.FromResult<StudentWalletSummary?>(wallet);
        }
    }

    public Task<HolderDashboard> GetHolderDashboardAsync(
        string walletAddress,
        string did,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var profile = ToHolderProfile(walletAddress, did);
            var institutions = _studentWallets.Values
                .Where(wallet =>
                    string.Equals(wallet.WalletAddress, walletAddress, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(wallet.Status, "active", StringComparison.OrdinalIgnoreCase))
                .Join(
                    _students.Values.Where(student => student.IsActive),
                    wallet => wallet.StudentId,
                    student => student.Id,
                    (wallet, student) => new { wallet, student })
                .Join(
                    _institutions.Values.Where(institution => institution.IsActive),
                    row => row.student.InstitutionId,
                    institution => institution.Id,
                    (row, institution) => new HolderInstitutionSummary(
                        institution.Id,
                        institution.Code,
                        institution.DisplayName,
                        row.student.Id,
                        row.student.ExternalReference,
                        row.student.EnrollmentYear,
                        row.wallet.WalletAddress,
                        row.wallet.Did,
                        row.wallet.IsPrimary,
                        row.wallet.ActivatedAt))
                .OrderBy(institution => institution.InstitutionName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Task.FromResult(new HolderDashboard(profile, institutions));
        }
    }

    public Task<HolderProfile> UpdateHolderProfileAsync(
        UpdateHolderProfileCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var userId = _usersByWallet.TryGetValue(command.WalletAddress, out var existingUserId)
                ? existingUserId
                : Guid.NewGuid();

            _usersByWallet[command.WalletAddress] = userId;
            _users[userId] = new UserState(
                userId,
                command.WalletAddress,
                command.Did,
                command.ContactEmail,
                command.DisplayName);
            _holderProfiles[userId] = new HolderProfileState(
                command.FullName,
                command.BirthDate,
                command.ContactEmail,
                command.CountryCode,
                command.PhoneNumber,
                now);

            return Task.FromResult(ToHolderProfile(command.WalletAddress, command.Did));
        }
    }

    public Task<InstitutionInvitationCreated> CreateInvitationAsync(
        CreateInstitutionInvitationCommand command,
        string tokenHash,
        string invitationUrl,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var invitation = new InstitutionInvitationCreated(
                Guid.NewGuid(),
                command.InstitutionId,
                command.Email,
                command.Role,
                invitationUrl,
                expiresAt);

            _invitations.Add(invitation.Id, new InvitationState(invitation, tokenHash, false, null));
            return Task.FromResult(invitation);
        }
    }

    public Task<InvitationAcceptResult> AcceptInvitationAsync(
        string tokenHash,
        string walletAddress,
        string did,
        string? displayName,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var state = _invitations.Values.SingleOrDefault(i =>
                string.Equals(i.TokenHash, tokenHash, StringComparison.Ordinal)
                && !i.Accepted
                && i.Invitation.ExpiresAt > now);

            if (state is null)
            {
                return Task.FromResult(new InvitationAcceptResult(null, null));
            }

            var userByEmail = _users.Values.SingleOrDefault(user =>
                user.Email is not null
                && string.Equals(user.Email, state.Invitation.Email, StringComparison.OrdinalIgnoreCase));

            Guid userId;
            if (_usersByWallet.TryGetValue(walletAddress, out var existingUserId))
            {
                userId = existingUserId;
                var existingUser = _users[userId];
                _users[userId] = existingUser with
                {
                    Email = existingUser.Email ?? state.Invitation.Email,
                    DisplayName = existingUser.DisplayName ?? displayName
                };
            }
            else if (userByEmail is not null)
            {
                if (!string.Equals(userByEmail.WalletAddress, walletAddress, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new InvitationAcceptResult(null, "invitation_wallet_email_mismatch"));
                }

                userId = userByEmail.Id;
            }
            else
            {
                userId = Guid.NewGuid();
                _usersByWallet[walletAddress] = userId;
                _users[userId] = new UserState(userId, walletAddress, did, state.Invitation.Email, displayName);
            }

            var institutionUser = new InstitutionUserSummary(
                Guid.NewGuid(),
                state.Invitation.InstitutionId,
                userId,
                walletAddress,
                did,
                state.Invitation.Email,
                displayName,
                state.Invitation.Role,
                now,
                null);
            _institutionUsers[institutionUser.Id] = institutionUser;

            if (string.Equals(state.Invitation.Role, InstitutionRoles.Admin, StringComparison.OrdinalIgnoreCase)
                || string.Equals(state.Invitation.Role, InstitutionRoles.Issuer, StringComparison.OrdinalIgnoreCase))
            {
                if (_institutions.TryGetValue(state.Invitation.InstitutionId, out var institution)
                    && string.IsNullOrEmpty(institution.IssuerWalletAddress))
                {
                    _institutions[state.Invitation.InstitutionId] = institution with
                    {
                        IssuerWalletAddress = walletAddress,
                        Did = institution.Did ?? did
                    };
                }
            }

            var accepted = new InstitutionInvitationAccepted(
                state.Invitation.InstitutionId,
                userId,
                walletAddress,
                did,
                state.Invitation.Role);

            _invitations[state.Invitation.Id] = state with { Accepted = true, AcceptedByUserId = userId };
            return Task.FromResult(new InvitationAcceptResult(accepted, null));
        }
    }

    public Task<IReadOnlyList<InstitutionUserSummary>> ListInstitutionUsersAsync(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<InstitutionUserSummary>>(
                _institutionUsers.Values
                    .Where(user => user.InstitutionId == institutionId && user.RevokedAt is null)
                    .OrderBy(user => user.Email, StringComparer.OrdinalIgnoreCase)
                    .ToList());
        }
    }

    public Task<InstitutionUserSummary?> UpdateInstitutionUserRoleAsync(
        Guid institutionId,
        Guid userId,
        string role,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var current = _institutionUsers.Values.FirstOrDefault(user =>
                user.InstitutionId == institutionId
                && user.UserId == userId
                && user.RevokedAt is null);

            if (current is null)
            {
                return Task.FromResult<InstitutionUserSummary?>(null);
            }

            var updated = current with { Role = role };
            _institutionUsers[current.Id] = updated;
            return Task.FromResult<InstitutionUserSummary?>(updated);
        }
    }

    public Task<bool> RevokeInstitutionUserAsync(
        Guid institutionId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var current = _institutionUsers.Values.FirstOrDefault(user =>
                user.InstitutionId == institutionId
                && user.UserId == userId
                && user.RevokedAt is null);

            if (current is null)
            {
                return Task.FromResult(false);
            }

            _institutionUsers[current.Id] = current with { RevokedAt = now };
            return Task.FromResult(true);
        }
    }

    private sealed record InvitationState(
        InstitutionInvitationCreated Invitation,
        string TokenHash,
        bool Accepted,
        Guid? AcceptedByUserId);

    private sealed record UserState(
        Guid Id,
        string WalletAddress,
        string Did,
        string? Email,
        string? DisplayName);

    private HolderProfile ToHolderProfile(string walletAddress, string did)
    {
        if (!_usersByWallet.TryGetValue(walletAddress, out var userId)
            || !_users.TryGetValue(userId, out var user))
        {
            return new HolderProfile(walletAddress, did, null, null, null, null, null, null, null);
        }

        _holderProfiles.TryGetValue(userId, out var profile);
        return new HolderProfile(
            user.WalletAddress,
            user.Did,
            user.DisplayName,
            profile?.FullName,
            profile?.BirthDate,
            profile?.ContactEmail,
            profile?.CountryCode,
            profile?.PhoneNumber,
            profile?.UpdatedAt);
    }

    private InstitutionSummary EnrichInstitutionSummary(InstitutionSummary institution)
    {
        if (!string.IsNullOrEmpty(institution.IssuerWalletAddress))
        {
            return institution;
        }

        var fallback = FindIssuerWalletFallback(institution.Id);
        return fallback is null
            ? institution
            : institution with
            {
                IssuerWalletAddress = fallback.Value.WalletAddress,
                Did = fallback.Value.Did ?? institution.Did
            };
    }

    private (string WalletAddress, string Did)? FindIssuerWalletFallback(Guid institutionId)
    {
        var institutionUser = _institutionUsers.Values
            .Where(user =>
                user.InstitutionId == institutionId
                && user.RevokedAt is null
                && (string.Equals(user.Role, InstitutionRoles.Admin, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(user.Role, InstitutionRoles.Issuer, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(user => user.GrantedAt)
            .FirstOrDefault();

        return institutionUser is null
            ? null
            : (institutionUser.WalletAddress, institutionUser.Did);
    }

    private sealed record HolderProfileState(
        string? FullName,
        DateOnly? BirthDate,
        string? ContactEmail,
        string? CountryCode,
        string? PhoneNumber,
        DateTimeOffset UpdatedAt);
}

