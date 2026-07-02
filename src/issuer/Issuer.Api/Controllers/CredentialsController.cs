using Issuer.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Issuer.Api.Controllers;

[ApiController]
[Route("issuer/credentials")]
[Authorize]
[Produces("application/json")]
public sealed class CredentialsController : ControllerBase
{
    private readonly GetHolderCredentialUseCase _getCredential;

    public CredentialsController(GetHolderCredentialUseCase getCredential) =>
        _getCredential = getCredential;

    /// <summary>Devuelve el detalle de una credencial autenticada si pertenece al titular del JWT.</summary>
    [HttpGet("{credentialId:guid}")]
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
}
