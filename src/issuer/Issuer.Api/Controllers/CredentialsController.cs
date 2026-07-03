using Issuer.Api.Models;
using Issuer.Application;
using Issuer.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SovereignID.Authorization;

namespace Issuer.Api.Controllers;

[ApiController]
[Route("issuer/credentials")]
[Produces("application/json")]
public sealed class CredentialsController : ControllerBase
{
    private readonly GetHolderCredentialUseCase _getCredential;
    private readonly IssuerService _issuerService;

    public CredentialsController(
        GetHolderCredentialUseCase getCredential,
        IssuerService issuerService)
    {
        _getCredential = getCredential;
        _issuerService = issuerService;
    }

    /// <summary>Devuelve el detalle de una credencial autenticada si pertenece al titular del JWT.</summary>
    [HttpGet("{credentialId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.HolderAuthenticated)]
    [ProducesResponseType(typeof(HolderCredentialDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HolderCredentialDetail>> Get(
        Guid credentialId,
        CancellationToken cancellationToken)
    {
        var result = await _getCredential.ExecuteAsync(credentialId, cancellationToken);
        return result switch
        {
            IssuerSuccess<HolderCredentialDetail> success => Ok(success.Value),
            IssuerFailureResult<HolderCredentialDetail> failure => throw new IssuerFailureException(failure.Failure),
            _ => throw new InvalidOperationException("Unexpected issuer result.")
        };
    }

    /// <summary>Revokes an active credential after on-chain revocation.</summary>
    [HttpPost("{credentialId:guid}/revoke")]
    [Authorize(Policy = AuthorizationPolicies.InstitutionIssuer)]
    [ProducesResponseType(typeof(CredentialRevoked), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CredentialRevoked>> RevokeCredential(
        Guid credentialId,
        [FromBody] RevokeCredentialRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _issuerService.RevokeCredentialAsync(
            new RevokeCredentialCommand(
                credentialId,
                request.Reason,
                request.RevocationTxHash,
                request.BlockNumber,
                request.ChainId,
                request.Eip712Signature,
                request.RevokedByUserId),
            cancellationToken);

        return result switch
        {
            IssuerSuccess<CredentialRevoked> success => Ok(success.Value),
            IssuerFailureResult<CredentialRevoked> failure => throw new IssuerFailureException(failure.Failure),
            _ => throw new InvalidOperationException("Unexpected issuer result.")
        };
    }
}
