using Microsoft.AspNetCore.Mvc;
using Verifier.Api.Models;
using Verifier.Application;
using Verifier.Domain;

namespace Verifier.Api.Controllers;

[ApiController]
[Route("verifications")]
[Produces("application/json")]
public sealed class VerificationsController : ControllerBase
{
    private readonly VerifyCredentialUseCase _verifyCredentialUseCase;

    public VerificationsController(VerifyCredentialUseCase verifyCredentialUseCase) =>
        _verifyCredentialUseCase = verifyCredentialUseCase;

    /// <summary>Verifica una Verifiable Credential por su UUID (verificación pública, sin autenticación).</summary>
    /// <remarks>
    /// Los veredictos de negocio (válida/revocada/expirada/inexistente) se devuelven con <c>200</c> y el campo
    /// <c>result</c>. El único error de protocolo es <c>invalid_credential_id</c> (<c>400</c>, RFC 7807 Problem Details
    /// con extensión <c>error</c>) cuando <c>credentialId</c> está ausente o no es un UUID válido.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(VerificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VerificationResponse>> Verify(
        [FromBody] VerificationRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.CredentialId)
            || !Guid.TryParse(request.CredentialId, out var credentialId))
        {
            throw new VerifierFailureException(
                StatusCodes.Status400BadRequest,
                VerifierErrorCodes.InvalidCredentialId,
                "credentialId is required and must be a valid UUID.",
                "Invalid credential id");
        }

        var outcome = await _verifyCredentialUseCase.ExecuteAsync(credentialId, cancellationToken);

        return Ok(ToResponse(outcome));
    }

    private static VerificationResponse ToResponse(VerificationOutcome outcome)
    {
        var checks = outcome.Verdict.Checks;

        return new VerificationResponse(
            outcome.Verdict.Result.ToWireValue(),
            new VerificationChecksResponse(
                checks.Found,
                checks.NotRevoked,
                checks.NotExpired,
                checks.HashMatches,
                checks.OnChainExists,
                checks.SignatureValid),
            outcome.Credential is { } credential
                ? new CredentialResponse(
                    credential.Id,
                    credential.TypeCode,
                    credential.Status,
                    credential.IssuedAt,
                    credential.ExpiresAt,
                    credential.SubjectDid,
                    new IssuerResponse(credential.Issuer.Did, credential.Issuer.DisplayName, credential.Issuer.Code),
                    new AnchorsResponse(
                        credential.Anchors.IpfsCid,
                        credential.Anchors.ContentHash,
                        credential.Anchors.TransactionHash,
                        credential.Anchors.ChainId))
                : null);
    }
}
