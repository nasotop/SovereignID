namespace Verifier.Application;

public interface ICredentialEvidenceVerifier
{
    Task<CredentialEvidenceChecks> VerifyAsync(CredentialEvidence evidence, CancellationToken cancellationToken);
}
