using Microsoft.Extensions.Options;

namespace Issuer.Application;

public sealed class IssuerService
{
    private readonly ITitleIssuerRepository _repository;
    private readonly IBlockchainAnchorVerifier _blockchainVerifier;
    private readonly TimeProvider _timeProvider;
    private readonly IssuerOptions _options;

    public IssuerService(
        ITitleIssuerRepository repository,
        IBlockchainAnchorVerifier blockchainVerifier,
        TimeProvider timeProvider,
        IOptions<IssuerOptions> options)
    {
        _repository = repository;
        _blockchainVerifier = blockchainVerifier;
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
            ChainId = command.ChainId ?? _options.DefaultChainId,
            CredentialId = command.CredentialId == Guid.Empty ? null : command.CredentialId
        };

        var issuerWallet = await _repository.GetInstitutionIssuerWalletForStudentAsync(
            normalized.StudentId,
            cancellationToken);

        if (issuerWallet is null)
        {
            return Fail<StudentTitleLinked>(
                "title_link_failed",
                409,
                "Title could not be linked. Check that student, wallet, institution DID, career and credential type exist.");
        }

        var anchorCheck = await _blockchainVerifier.VerifyIssueAnchorAsync(
            new BlockchainAnchorProof(
                normalized.TransactionHash,
                normalized.BlockNumber,
                normalized.ChainId!.Value,
                normalized.ContentHash,
                normalized.Eip712Signature,
                normalized.CredentialId),
            issuerWallet,
            cancellationToken);

        if (!anchorCheck.IsValid)
        {
            return Fail<StudentTitleLinked>(
                anchorCheck.ErrorCode ?? "blockchain_anchor_invalid",
                409,
                anchorCheck.Detail ?? "Blockchain anchor verification failed.");
        }

        var linked = await _repository.LinkStudentTitleAsync(normalized, _timeProvider.GetUtcNow(), cancellationToken);
        return linked is null
            ? Fail<StudentTitleLinked>("title_link_failed", 409, "Title could not be linked. Check that student, wallet, institution DID, career and credential type exist.")
            : new IssuerSuccess<StudentTitleLinked>(linked);
    }

    public async Task<IssuerResult<IReadOnlyList<CredentialSummary>>> ListInstitutionCredentialsAsync(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        if (institutionId == Guid.Empty)
        {
            return Fail<IReadOnlyList<CredentialSummary>>("invalid_institution", 400, "institutionId is required.");
        }

        var credentials = await _repository.ListInstitutionCredentialsAsync(institutionId, cancellationToken);
        return new IssuerSuccess<IReadOnlyList<CredentialSummary>>(credentials);
    }

    public async Task<IssuerResult<CredentialSummary>> GetCredentialAsync(
        Guid credentialId,
        CancellationToken cancellationToken)
    {
        if (credentialId == Guid.Empty)
        {
            return Fail<CredentialSummary>("invalid_credential", 400, "credentialId is required.");
        }

        var credential = await _repository.GetCredentialAsync(credentialId, cancellationToken);
        return credential is null
            ? Fail<CredentialSummary>("credential_not_found", 404, "Credential was not found.")
            : new IssuerSuccess<CredentialSummary>(credential);
    }

    public async Task<IssuerResult<CredentialRevoked>> RevokeCredentialAsync(
        RevokeCredentialCommand command,
        CancellationToken cancellationToken)
    {
        if (command.CredentialId == Guid.Empty)
        {
            return Fail<CredentialRevoked>("invalid_credential", 400, "credentialId is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return Fail<CredentialRevoked>("invalid_revocation_reason", 400, "reason is required.");
        }

        if (string.IsNullOrWhiteSpace(command.RevocationTxHash)
            || string.IsNullOrWhiteSpace(command.Eip712Signature))
        {
            return Fail<CredentialRevoked>("invalid_revocation_payload", 400, "revocationTxHash and eip712Signature are required.");
        }

        var normalized = command with
        {
            Reason = command.Reason.Trim(),
            RevocationTxHash = command.RevocationTxHash.Trim(),
            Eip712Signature = command.Eip712Signature.Trim(),
            ChainId = command.ChainId ?? _options.DefaultChainId
        };

        var existing = await _repository.GetCredentialAsync(normalized.CredentialId, cancellationToken);
        if (existing is null)
        {
            return Fail<CredentialRevoked>("credential_not_found", 404, "Credential was not found.");
        }

        if (!string.Equals(existing.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return Fail<CredentialRevoked>("credential_not_active", 409, "Only active credentials can be revoked.");
        }

        var issuerWallet = await _repository.GetInstitutionIssuerWalletAsync(existing.InstitutionId, cancellationToken);
        if (issuerWallet is null)
        {
            return Fail<CredentialRevoked>("issuer_wallet_not_found", 409, "Institution issuer wallet is not configured.");
        }

        var anchorCheck = await _blockchainVerifier.VerifyRevocationAnchorAsync(
            new BlockchainAnchorProof(
                normalized.RevocationTxHash,
                normalized.BlockNumber,
                normalized.ChainId!.Value,
                existing.ContentHash,
                normalized.Eip712Signature,
                normalized.CredentialId),
            issuerWallet,
            normalized.CredentialId,
            cancellationToken);

        if (!anchorCheck.IsValid)
        {
            return Fail<CredentialRevoked>(
                anchorCheck.ErrorCode ?? "blockchain_revocation_invalid",
                409,
                anchorCheck.Detail ?? "Blockchain revocation verification failed.");
        }

        var revoked = await _repository.RevokeCredentialAsync(normalized, _timeProvider.GetUtcNow(), cancellationToken);
        return revoked is null
            ? Fail<CredentialRevoked>("revocation_failed", 409, "Credential could not be revoked.")
            : new IssuerSuccess<CredentialRevoked>(revoked);
    }

    private static IssuerFailureResult<T> Fail<T>(string errorCode, int statusCode, string detail) =>
        new(new IssuerFailure(errorCode, statusCode, detail));

    private static string? BlankToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
