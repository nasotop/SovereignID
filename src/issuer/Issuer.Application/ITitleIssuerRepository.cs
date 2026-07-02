namespace Issuer.Application;

public interface ITitleIssuerRepository
{
    Task<InstitutionIssuerWalletLinked?> LinkInstitutionIssuerWalletAsync(
        LinkInstitutionIssuerWalletCommand command,
        CancellationToken cancellationToken);

    Task<StudentTitleLinked?> LinkStudentTitleAsync(
        LinkStudentTitleCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CredentialSummary>> ListInstitutionCredentialsAsync(
        Guid institutionId,
        CancellationToken cancellationToken);

    Task<CredentialSummary?> GetCredentialAsync(
        Guid credentialId,
        CancellationToken cancellationToken);

    Task<CredentialRevoked?> RevokeCredentialAsync(
        RevokeCredentialCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<string?> GetInstitutionIssuerWalletAsync(
        Guid institutionId,
        CancellationToken cancellationToken);

    Task<string?> GetInstitutionIssuerWalletForStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken);
}
