using Issuer.Api.Models;
using Issuer.Application;
using Issuer.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Issuer.Api.Controllers;

[ApiController]
[Route("issuer/credentials")]
[Produces("application/json")]
[Authorize(Policy = IssuerAuthorizationPolicy.IssuerPolicyName)]
public sealed class CredentialsController : ControllerBase
{
    private readonly IssuerService _issuerService;

    public CredentialsController(IssuerService issuerService)
    {
        _issuerService = issuerService;
    }

    /// <summary>Gets a credential by identifier.</summary>
    [HttpGet("{credentialId:guid}")]
    [ProducesResponseType(typeof(CredentialSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CredentialSummary>> GetCredential(
        Guid credentialId,
        CancellationToken cancellationToken)
    {
        var result = await _issuerService.GetCredentialAsync(credentialId, cancellationToken);
        return FromResult(result, success => (ActionResult<CredentialSummary>)Ok(success));
    }

    /// <summary>Revokes an active credential after on-chain revocation.</summary>
    [HttpPost("{credentialId:guid}/revoke")]
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

        return FromResult(result, success => (ActionResult<CredentialRevoked>)Ok(success));
    }

    private static ActionResult<T> FromResult<T>(
        IssuerResult<T> result,
        Func<T, ActionResult<T>> onSuccess) =>
        result switch
        {
            IssuerSuccess<T> success => onSuccess(success.Value),
            IssuerFailureResult<T> failure => throw new IssuerFailureException(failure.Failure),
            _ => throw new InvalidOperationException("Unexpected issuer result.")
        };
}
