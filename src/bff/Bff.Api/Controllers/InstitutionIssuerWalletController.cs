using Bff.Api;
using Bff.Api.Models;
using Microsoft.AspNetCore.Mvc;
using KiotaIssuer = SovereignID.Bff.Clients.Issuer.Models;
using IssuerApiClient = SovereignID.Bff.Clients.Issuer.ApiClient;

namespace Bff.Api.Controllers;

[ApiController]
[Route("issuer/institutions")]
[Produces("application/json")]
public sealed class InstitutionIssuerWalletController(IssuerApiClient issuer) : ControllerBase
{
    [HttpPost("{institutionId:guid}/wallet")]
    [ProducesResponseType(typeof(InstitutionIssuerWalletLinked), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> LinkWallet(
        Guid institutionId,
        [FromBody] LinkInstitutionIssuerWalletRequest request,
        CancellationToken cancellationToken)
    {
        var kiotaRequest = new KiotaIssuer.LinkInstitutionIssuerWalletRequest
        {
            WalletAddress = request.WalletAddress,
            Did = request.Did,
            PublicKey = request.PublicKey,
        };

        return DownstreamResults.OkMappedAsync(
            () => issuer.Issuer.Institutions[institutionId].Wallet
                .PostAsync(kiotaRequest, cancellationToken: cancellationToken),
            KiotaWireMappers.ToWire);
    }
}
