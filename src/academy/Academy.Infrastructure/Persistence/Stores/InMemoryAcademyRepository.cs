using Academy.Application;

namespace Academy.Infrastructure.Persistence.Stores;

internal sealed class InMemoryAcademyRepository : IAcademyRepository
{
    private readonly Lock _lock = new();
    private readonly Dictionary<Guid, InstitutionSummary> _institutions = [];
    private readonly Dictionary<Guid, CareerSummary> _careers = [];
    private readonly Dictionary<Guid, StudentSummary> _students = [];
    private readonly Dictionary<Guid, InvitationState> _invitations = [];
    private readonly Dictionary<string, Guid> _usersByWallet = [];
    private readonly HashSet<(Guid InstitutionId, Guid UserId, string Role)> _institutionUsers = [];

    public Task<bool> InstitutionCodeExistsAsync(string code, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(_institutions.Values.Any(i => string.Equals(i.Code, code, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<InstitutionSummary?> GetInstitutionAsync(Guid institutionId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(_institutions.GetValueOrDefault(institutionId));
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

    public Task<bool> CareerCodeExistsAsync(Guid institutionId, string code, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(_careers.Values.Any(c => c.InstitutionId == institutionId && string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase)));
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
            return Task.FromResult(student);
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

    public Task<InstitutionInvitationAccepted?> AcceptInvitationAsync(
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
                return Task.FromResult<InstitutionInvitationAccepted?>(null);
            }

            var userId = _usersByWallet.TryGetValue(walletAddress, out var existingUserId)
                ? existingUserId
                : Guid.NewGuid();

            _usersByWallet[walletAddress] = userId;
            _institutionUsers.Add((state.Invitation.InstitutionId, userId, state.Invitation.Role));

            var accepted = new InstitutionInvitationAccepted(
                state.Invitation.InstitutionId,
                userId,
                walletAddress,
                did,
                state.Invitation.Role);

            _invitations[state.Invitation.Id] = state with { Accepted = true, AcceptedByUserId = userId };
            return Task.FromResult<InstitutionInvitationAccepted?>(accepted);
        }
    }

    private sealed record InvitationState(
        InstitutionInvitationCreated Invitation,
        string TokenHash,
        bool Accepted,
        Guid? AcceptedByUserId);
}

