using Bff.Api;
using Bff.Api.Models;
using Microsoft.AspNetCore.Mvc;
using KiotaIssuer = SovereignID.Bff.Clients.Issuer.Models;
using IssuerApiClient = SovereignID.Bff.Clients.Issuer.ApiClient;

namespace Bff.Api.Controllers;

[ApiController]
[Route("issuer/institutions")]
[Produces("application/json")]
public sealed class InstitutionDocumentsController(IssuerApiClient issuer) : ControllerBase
{
    [HttpPost("{institutionId:guid}/documents/anchor")]
    [ProducesResponseType(typeof(ContentAnchorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> AnchorDocument(
        Guid institutionId,
        [FromBody] AnchorCredentialDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var kiotaRequest = new KiotaIssuer.AnchorCredentialDocumentRequest
        {
            Document = KiotaWireMappers.ToUntypedNode(request.Document),
        };

        return DownstreamResults.OkMappedAsync(
            () => issuer.Issuer.Institutions[institutionId].Documents.Anchor
                .PostAsync(kiotaRequest, cancellationToken: cancellationToken),
            KiotaWireMappers.ToWire);
    }
}
