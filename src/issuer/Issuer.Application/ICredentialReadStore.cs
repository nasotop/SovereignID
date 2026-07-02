namespace Issuer.Application;

public interface ICredentialReadStore
{
    Task<IReadOnlyList<HolderCredentialSummary>> ListBySubjectDidAsync(
        string subjectDid,
        CancellationToken cancellationToken);

    Task<HolderCredentialDetail?> GetByIdForSubjectAsync(
        Guid credentialId,
        string subjectDid,
        CancellationToken cancellationToken);
}
