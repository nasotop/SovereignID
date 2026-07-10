namespace Academy.Application;

public interface IAcademyRepository
{
    Task<bool> InstitutionCodeExistsAsync(string code, CancellationToken cancellationToken);

    Task<IReadOnlyList<InstitutionSummary>> ListInstitutionsAsync(CancellationToken cancellationToken);

    Task<InstitutionSummary?> GetInstitutionAsync(Guid institutionId, CancellationToken cancellationToken);

    Task<InstitutionSummary> CreateInstitutionAsync(
        CreateInstitutionCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> CareerCodeExistsAsync(
        Guid institutionId,
        string code,
        CancellationToken cancellationToken);

    Task<bool> CareerCodeExistsAsync(
        Guid institutionId,
        string code,
        Guid excludingCareerId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CareerSummary>> ListCareersAsync(
        Guid institutionId,
        CancellationToken cancellationToken);

    Task<CareerSummary?> GetCareerAsync(
        Guid institutionId,
        Guid careerId,
        CancellationToken cancellationToken);

    Task<CareerSummary> CreateCareerAsync(
        CreateCareerCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<CareerSummary?> UpdateCareerAsync(
        UpdateCareerCommand command,
        CancellationToken cancellationToken);

    Task<CareerSummary?> DeactivateCareerAsync(
        Guid institutionId,
        Guid careerId,
        CancellationToken cancellationToken);

    Task<bool> StudentExternalReferenceExistsAsync(
        Guid institutionId,
        string externalReference,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StudentSummary>> ListStudentsAsync(Guid institutionId, CancellationToken cancellationToken);

    Task<StudentSummary?> GetStudentAsync(Guid institutionId, Guid studentId, CancellationToken cancellationToken);

    Task<StudentSummary> CreateStudentAsync(
        CreateStudentCommand command,
        string? walletDid,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<StudentWalletSummary?> AddStudentWalletAsync(
        AddStudentWalletCommand command,
        string did,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<HolderDashboard> GetHolderDashboardAsync(
        string walletAddress,
        string did,
        CancellationToken cancellationToken);

    Task<HolderProfile> UpdateHolderProfileAsync(
        UpdateHolderProfileCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<InstitutionUserSummary>> ListInstitutionUsersAsync(
        Guid institutionId,
        CancellationToken cancellationToken);

    Task<InstitutionUserSummary?> UpdateInstitutionUserRoleAsync(
        Guid institutionId,
        Guid userId,
        string role,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> RevokeInstitutionUserAsync(
        Guid institutionId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<InstitutionInvitationCreated> CreateInvitationAsync(
        CreateInstitutionInvitationCommand command,
        string tokenHash,
        string invitationUrl,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<InvitationAcceptResult> AcceptInvitationAsync(
        string tokenHash,
        string walletAddress,
        string did,
        string? displayName,
        DateTimeOffset now,
        CancellationToken cancellationToken);

}

