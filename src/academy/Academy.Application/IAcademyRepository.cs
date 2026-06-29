namespace Academy.Application;

public interface IAcademyRepository
{
    Task<bool> InstitutionCodeExistsAsync(string code, CancellationToken cancellationToken);

    Task<InstitutionSummary?> GetInstitutionAsync(Guid institutionId, CancellationToken cancellationToken);

    Task<InstitutionSummary> CreateInstitutionAsync(
        CreateInstitutionCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> CareerCodeExistsAsync(
        Guid institutionId,
        string code,
        CancellationToken cancellationToken);

    Task<CareerSummary> CreateCareerAsync(
        CreateCareerCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> StudentExternalReferenceExistsAsync(
        Guid institutionId,
        string externalReference,
        CancellationToken cancellationToken);

    Task<StudentSummary> CreateStudentAsync(
        CreateStudentCommand command,
        string? walletDid,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<InstitutionInvitationCreated> CreateInvitationAsync(
        CreateInstitutionInvitationCommand command,
        string tokenHash,
        string invitationUrl,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<InstitutionInvitationAccepted?> AcceptInvitationAsync(
        string tokenHash,
        string walletAddress,
        string did,
        string? displayName,
        DateTimeOffset now,
        CancellationToken cancellationToken);

}

