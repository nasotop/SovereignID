using Issuer.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Issuer.Api.Controllers;

[ApiController]
[Route("issuer/holders/me/credentials")]
[Authorize]
[Produces("application/json")]
public sealed class HolderCredentialsController : ControllerBase
{
    private readonly ListHolderCredentialsUseCase _listCredentials;
    private readonly GetHolderCredentialUseCase _getCredential;

    public HolderCredentialsController(
        ListHolderCredentialsUseCase listCredentials,
        GetHolderCredentialUseCase getCredential)
    {
        _listCredentials = listCredentials;
        _getCredential = getCredential;
    }

    /// <summary>Lista las credenciales del titular autenticado (JWT SIWE).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HolderCredentialSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<HolderCredentialSummary>>> List(
        CancellationToken cancellationToken)
    {
        var result = await _listCredentials.ExecuteAsync(cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Devuelve el detalle de una credencial del titular autenticado.</summary>
    [HttpGet("{credentialId:guid}")]
    [ProducesResponseType(typeof(HolderCredentialDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HolderCredentialDetail>> Get(
        Guid credentialId,
        CancellationToken cancellationToken)
    {
        var result = await _getCredential.ExecuteAsync(credentialId, cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<T> ToActionResult<T>(IssuerResult<T> result) =>
        result switch
        {
            IssuerSuccess<T> success => Ok(success.Value),
            IssuerFailureResult<T> failure => throw new IssuerFailureException(failure.Failure),
            _ => throw new InvalidOperationException("Unexpected issuer result.")
        };
}
