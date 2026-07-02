namespace Issuer.Application;

public sealed class GetHolderCredentialUseCase
{
    private readonly ICredentialReadStore _readStore;
    private readonly IIssuerRequestContext _requestContext;

    public GetHolderCredentialUseCase(
        ICredentialReadStore readStore,
        IIssuerRequestContext requestContext)
    {
        _readStore = readStore;
        _requestContext = requestContext;
    }

    public async Task<IssuerResult<HolderCredentialDetail>> ExecuteAsync(
        Guid credentialId,
        CancellationToken cancellationToken)
    {
        if (!TryGetSubjectDid(out var subjectDid, out var failure))
        {
            return failure;
        }

        var credential = await _readStore.GetByIdForSubjectAsync(credentialId, subjectDid, cancellationToken);
        if (credential is null)
        {
            return new IssuerFailureResult<HolderCredentialDetail>(
                new IssuerFailure("credential_not_found", 404, "Credential was not found for the authenticated holder."));
        }

        return new IssuerSuccess<HolderCredentialDetail>(credential);
    }

    private bool TryGetSubjectDid(out string subjectDid, out IssuerFailureResult<HolderCredentialDetail> failure)
    {
        if (_requestContext.IsAuthenticated && !string.IsNullOrWhiteSpace(_requestContext.SubjectDid))
        {
            subjectDid = _requestContext.SubjectDid;
            failure = null!;
            return true;
        }

        subjectDid = string.Empty;
        failure = new IssuerFailureResult<HolderCredentialDetail>(
            new IssuerFailure("unauthenticated", 401, "Authentication is required."));
        return false;
    }
}
