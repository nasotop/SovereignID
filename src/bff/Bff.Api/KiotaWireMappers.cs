using Bff.Api.Models;
using KiotaVerifier = SovereignID.Bff.Clients.Verifier.Models;
using KiotaIssuer = SovereignID.Bff.Clients.Issuer.Models;

namespace Bff.Api;

internal static class KiotaWireMappers
{
    public static VerificationResponse ToWire(KiotaVerifier.VerificationResponse response)
    {
        var checks = response.Checks;
        var credential = response.Credential?.CredentialResponse;

        return new VerificationResponse(
            ToVerifierResultWire(response.Result),
            new VerificationChecksResponse(
                checks?.Found,
                checks?.NotRevoked,
                checks?.NotExpired,
                checks?.HashMatches,
                checks?.OnChainExists,
                checks?.SignatureValid),
            credential is null
                ? null
                : new CredentialResponse(
                    credential.Id ?? Guid.Empty,
                    credential.Type ?? string.Empty,
                    credential.Status ?? string.Empty,
                    credential.IssuedAt ?? DateTimeOffset.MinValue,
                    credential.ExpiresAt,
                    credential.SubjectDid ?? string.Empty,
                    new IssuerResponse(
                        credential.Issuer?.Did ?? string.Empty,
                        credential.Issuer?.DisplayName ?? string.Empty,
                        credential.Issuer?.Code ?? string.Empty),
                    new AnchorsResponse(
                        credential.Anchors?.IpfsCid ?? string.Empty,
                        credential.Anchors?.ContentHash ?? string.Empty,
                        credential.Anchors?.TransactionHash ?? string.Empty,
                        int.TryParse(credential.Anchors?.ChainId?.ToString(), out var chainId) ? chainId : 0)));
    }

    public static HolderCredentialSummary ToWire(KiotaIssuer.HolderCredentialSummary summary) =>
        new(
            summary.Id ?? Guid.Empty,
            summary.Title ?? string.Empty,
            summary.IssuerName ?? string.Empty,
            summary.TypeCode ?? string.Empty,
            ToCredentialStatusWire(summary.Status),
            summary.IssuedAt ?? DateTimeOffset.MinValue,
            summary.ExpiresAt);

    public static HolderCredentialDetail ToWire(KiotaIssuer.HolderCredentialDetail detail) =>
        new(
            detail.Id ?? Guid.Empty,
            detail.Title ?? string.Empty,
            detail.IssuerName ?? string.Empty,
            detail.TypeCode ?? string.Empty,
            ToCredentialStatusWire(detail.Status),
            detail.IssuedAt ?? DateTimeOffset.MinValue,
            detail.ExpiresAt,
            detail.RevokedAt,
            detail.SubjectDid ?? string.Empty,
            new HolderCredentialIssuer(
                detail.Issuer?.Did ?? string.Empty,
                detail.Issuer?.DisplayName ?? string.Empty,
                detail.Issuer?.Code ?? string.Empty),
            new HolderCredentialAnchors(
                detail.Anchors?.IpfsCid ?? string.Empty,
                detail.Anchors?.IpfsGatewayUrl ?? string.Empty,
                detail.Anchors?.ContentHash ?? string.Empty,
                detail.Anchors?.TransactionHash ?? string.Empty,
                long.TryParse(detail.Anchors?.BlockNumber?.ToString(), out var block) ? block : 0,
                int.TryParse(detail.Anchors?.ChainId?.ToString(), out var chainId) ? chainId : 0,
                detail.Anchors?.Eip712Signature ?? string.Empty),
            detail.Metadata?.ToString());

    public static InstitutionIssuerWalletLinked ToWire(KiotaIssuer.InstitutionIssuerWalletLinked linked) =>
        new(
            linked.InstitutionId ?? Guid.Empty,
            linked.WalletAddress ?? string.Empty,
            linked.Did ?? string.Empty);

    public static StudentTitleLinked ToWire(KiotaIssuer.StudentTitleLinked linked) =>
        new(
            linked.CredentialId ?? Guid.Empty,
            linked.InstitutionId ?? Guid.Empty,
            linked.StudentId ?? Guid.Empty,
            linked.CareerId,
            linked.IssuedToWalletId ?? Guid.Empty,
            linked.SubjectDid ?? string.Empty,
            linked.IssuerDid ?? string.Empty,
            linked.Status ?? string.Empty,
            linked.IssuedAt ?? DateTimeOffset.MinValue);

    public static KiotaIssuer.LinkStudentTitleRequest ToKiota(LinkStudentTitleRequest request) =>
        new()
        {
            CareerId = request.CareerId,
            CredentialTypeCode = request.CredentialTypeCode,
            IpfsCid = request.IpfsCid,
            IpfsGatewayUrl = request.IpfsGatewayUrl,
            ContentHash = request.ContentHash,
            TransactionHash = request.TransactionHash,
            Eip712Signature = request.Eip712Signature,
            ExpiresAt = request.ExpiresAt,
            AdditionalData = new Dictionary<string, object?>
            {
                ["blockNumber"] = request.BlockNumber,
                ["chainId"] = request.ChainId ?? 0,
                ["metadata"] = request.Metadata,
            },
        };

    private static string ToVerifierResultWire(KiotaVerifier.VerificationResponse_result? result) =>
        result switch
        {
            KiotaVerifier.VerificationResponse_result.Valid => "valid",
            KiotaVerifier.VerificationResponse_result.Revoked => "revoked",
            KiotaVerifier.VerificationResponse_result.Expired => "expired",
            KiotaVerifier.VerificationResponse_result.Not_found => "not_found",
            _ => "not_found",
        };

    private static string ToCredentialStatusWire(KiotaIssuer.HolderCredentialSummary_status? status) =>
        status switch
        {
            KiotaIssuer.HolderCredentialSummary_status.Active => "active",
            KiotaIssuer.HolderCredentialSummary_status.Revoked => "revoked",
            KiotaIssuer.HolderCredentialSummary_status.Expired => "expired",
            _ => string.Empty,
        };

    private static string ToCredentialStatusWire(KiotaIssuer.HolderCredentialDetail_status? status) =>
        status switch
        {
            KiotaIssuer.HolderCredentialDetail_status.Active => "active",
            KiotaIssuer.HolderCredentialDetail_status.Revoked => "revoked",
            KiotaIssuer.HolderCredentialDetail_status.Expired => "expired",
            _ => string.Empty,
        };
}
