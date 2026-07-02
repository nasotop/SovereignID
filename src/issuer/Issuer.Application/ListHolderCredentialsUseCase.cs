namespace Issuer.Application;

public sealed class ListHolderCredentialsUseCase
{
    private readonly ICredentialReadStore _readStore;
    private readonly IIssuerRequestContext _requestContext;

    public ListHolderCredentialsUseCase(
        ICredentialReadStore readStore,
        IIssuerRequestContext requestContext)
    {
        _readStore = readStore;
        _requestContext = requestContext;
    }

    public async Task<IssuerResult<IReadOnlyList<HolderCredentialSummary>>> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        if (!TryGetSubjectDid(out var subjectDid, out var failure))
        {
            return failure;
        }

        var credentials = await _readStore.ListBySubjectDidAsync(subjectDid, cancellationToken);
        return new IssuerSuccess<IReadOnlyList<HolderCredentialSummary>>(credentials);
    }

    private bool TryGetSubjectDid(out string subjectDid, out IssuerFailureResult<IReadOnlyList<HolderCredentialSummary>> failure)
    {
        if (_requestContext.IsAuthenticated && !string.IsNullOrWhiteSpace(_requestContext.SubjectDid))
        {
            subjectDid = _requestContext.SubjectDid;
            failure = null!;
            return true;
        }

        subjectDid = string.Empty;
        failure = new IssuerFailureResult<IReadOnlyList<HolderCredentialSummary>>(
            new IssuerFailure("unauthenticated", 401, "Authentication is required."));
        return false;
    }
}
