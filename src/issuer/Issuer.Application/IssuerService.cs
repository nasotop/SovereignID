using Microsoft.Extensions.Options;

namespace Issuer.Application;

public sealed class IssuerService
{
    private readonly ITitleIssuerRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly IssuerOptions _options;

    public IssuerService(
        ITitleIssuerRepository repository,
        TimeProvider timeProvider,
        IOptions<IssuerOptions> options)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<IssuerResult<InstitutionIssuerWalletLinked>> LinkInstitutionIssuerWalletAsync(
        LinkInstitutionIssuerWalletCommand command,
        CancellationToken cancellationToken)
    {
        if (command.InstitutionId == Guid.Empty)
        {
            return Fail<InstitutionIssuerWalletLinked>("invalid_institution", 400, "institutionId is required.");
        }

        if (string.IsNullOrWhiteSpace(command.WalletAddress) || string.IsNullOrWhiteSpace(command.Did))
        {
            return Fail<InstitutionIssuerWalletLinked>("invalid_issuer_wallet", 400, "walletAddress and did are required.");
        }

        var normalized = command with
        {
            WalletAddress = command.WalletAddress.Trim(),
            Did = command.Did.Trim(),
            PublicKey = BlankToNull(command.PublicKey)
        };

        var linked = await _repository.LinkInstitutionIssuerWalletAsync(normalized, cancellationToken);
        return linked is null
            ? Fail<InstitutionIssuerWalletLinked>("issuer_wallet_link_failed", 409, "Issuer wallet could not be linked. Check that institution exists and is active.")
            : new IssuerSuccess<InstitutionIssuerWalletLinked>(linked);
    }

    public async Task<IssuerResult<StudentTitleLinked>> LinkStudentTitleAsync(
        LinkStudentTitleCommand command,
        CancellationToken cancellationToken)
    {
        if (command.StudentId == Guid.Empty)
        {
            return Fail<StudentTitleLinked>("invalid_student", 400, "studentId is required.");
        }

        if (string.IsNullOrWhiteSpace(command.CredentialTypeCode))
        {
            return Fail<StudentTitleLinked>("invalid_credential_type", 400, "credentialTypeCode is required.");
        }

        if (string.IsNullOrWhiteSpace(command.IpfsCid)
            || string.IsNullOrWhiteSpace(command.IpfsGatewayUrl)
            || string.IsNullOrWhiteSpace(command.ContentHash)
            || string.IsNullOrWhiteSpace(command.TransactionHash)
            || string.IsNullOrWhiteSpace(command.Eip712Signature))
        {
            return Fail<StudentTitleLinked>("invalid_title_payload", 400, "IPFS, hash, transaction and signature data are required.");
        }

        var normalized = command with
        {
            CredentialTypeCode = command.CredentialTypeCode.Trim().ToUpperInvariant(),
            IpfsCid = command.IpfsCid.Trim(),
            IpfsGatewayUrl = command.IpfsGatewayUrl.Trim(),
            ContentHash = command.ContentHash.Trim(),
            TransactionHash = command.TransactionHash.Trim(),
            Eip712Signature = command.Eip712Signature.Trim(),
            ChainId = command.ChainId ?? _options.DefaultChainId
        };

        var linked = await _repository.LinkStudentTitleAsync(normalized, _timeProvider.GetUtcNow(), cancellationToken);
        return linked is null
            ? Fail<StudentTitleLinked>("title_link_failed", 409, "Title could not be linked. Check that student, wallet, institution DID, career and credential type exist.")
            : new IssuerSuccess<StudentTitleLinked>(linked);
    }

    private static IssuerFailureResult<T> Fail<T>(string errorCode, int statusCode, string detail) =>
        new(new IssuerFailure(errorCode, statusCode, detail));

    private static string? BlankToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
