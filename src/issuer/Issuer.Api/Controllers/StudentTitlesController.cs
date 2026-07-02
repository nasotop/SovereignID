using Issuer.Api.Models;
using Issuer.Application;
using Issuer.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Issuer.Api.Controllers;

[ApiController]
[Route("issuer/students")]
[Produces("application/json")]
[Authorize(Policy = IssuerAuthorizationPolicy.IssuerPolicyName)]
public sealed class StudentTitlesController : ControllerBase
{
    private readonly IssuerService _issuerService;

    public StudentTitlesController(IssuerService issuerService)
    {
        _issuerService = issuerService;
    }

    /// <summary>Vincula un titulo emitido a un estudiante con wallet primaria activa.</summary>
    [HttpPost("{studentId:guid}/title")]
    [ProducesResponseType(typeof(StudentTitleLinked), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudentTitleLinked>> LinkTitle(
        Guid studentId,
        [FromBody] LinkStudentTitleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _issuerService.LinkStudentTitleAsync(
            new LinkStudentTitleCommand(
                studentId,
                request.CareerId,
                request.CredentialTypeCode,
                request.IpfsCid,
                request.IpfsGatewayUrl,
                request.ContentHash,
                request.TransactionHash,
                request.BlockNumber,
                request.ChainId,
                request.Eip712Signature,
                request.ExpiresAt,
                request.Metadata,
                request.CredentialId),
            cancellationToken);

        return result switch
        {
            IssuerSuccess<StudentTitleLinked> success => Created($"/issuer/students/{studentId}/title/{success.Value.CredentialId}", success.Value),
            IssuerFailureResult<StudentTitleLinked> failure => throw new IssuerFailureException(failure.Failure),
            _ => throw new InvalidOperationException("Unexpected issuer result.")
        };
    }
}
