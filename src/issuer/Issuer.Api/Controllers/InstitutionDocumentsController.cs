using Issuer.Api.Models;
using Issuer.Application;
using Issuer.Application.ContentAnchor;
using Issuer.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Issuer.Api.Controllers;

[ApiController]
[Route("issuer/institutions")]
[Produces("application/json")]
[Authorize(Policy = IssuerAuthorizationPolicy.IssuerPolicyName)]
public sealed class InstitutionDocumentsController : ControllerBase
{
    private readonly IContentAnchorService _contentAnchorService;

    public InstitutionDocumentsController(IContentAnchorService contentAnchorService)
    {
        _contentAnchorService = contentAnchorService;
    }

    /// <summary>Canonicaliza (JCS), calcula contentHash y pinnea el VC JSON-LD en IPFS.</summary>
    [HttpPost("{institutionId:guid}/documents/anchor")]
    [ProducesResponseType(typeof(ContentAnchorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ContentAnchorResponse>> AnchorDocument(
        Guid institutionId,
        [FromBody] AnchorCredentialDocumentRequest request,
        CancellationToken cancellationToken)
    {
        _ = institutionId;

        var result = await _contentAnchorService.AnchorAsync(request.Document, cancellationToken);

        return result switch
        {
            IssuerSuccess<ContentAnchorResult> success => Ok(new ContentAnchorResponse(
                success.Value.ContentHash,
                success.Value.IpfsCid,
                success.Value.IpfsGatewayUrl)),
            IssuerFailureResult<ContentAnchorResult> failure => throw new IssuerFailureException(failure.Failure),
            _ => throw new InvalidOperationException("Unexpected content anchor result.")
        };
    }
}
