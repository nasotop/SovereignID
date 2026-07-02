using Issuer.Api.Models;
using Issuer.Application;
using Microsoft.AspNetCore.Mvc;

namespace Issuer.Api.Controllers;

[ApiController]
[Route("issuer/institutions")]
[Produces("application/json")]
public sealed class InstitutionIssuerWalletController : ControllerBase
{
    private readonly IssuerService _issuerService;

    public InstitutionIssuerWalletController(IssuerService issuerService)
    {
        _issuerService = issuerService;
    }

    /// <summary>Vincula la wallet/DID emisor de una institucion existente.</summary>
    [HttpPost("{institutionId:guid}/wallet")]
    [ProducesResponseType(typeof(InstitutionIssuerWalletLinked), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InstitutionIssuerWalletLinked>> LinkWallet(
        Guid institutionId,
        [FromBody] LinkInstitutionIssuerWalletRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _issuerService.LinkInstitutionIssuerWalletAsync(
            new LinkInstitutionIssuerWalletCommand(
                institutionId,
                request.WalletAddress,
                request.Did,
                request.PublicKey),
            cancellationToken);

        return result switch
        {
            IssuerSuccess<InstitutionIssuerWalletLinked> success => Ok(success.Value),
            IssuerFailureResult<InstitutionIssuerWalletLinked> failure => throw new IssuerFailureException(failure.Failure),
            _ => throw new InvalidOperationException("Unexpected issuer result.")
        };
    }
}
