using Issuer.Application;
using Microsoft.AspNetCore.Mvc;

namespace Issuer.Api.Controllers;

[ApiController]
[Route("issuer/institutions")]
[Produces("application/json")]
public sealed class InstitutionCredentialsController : ControllerBase
{
    private readonly IssuerService _issuerService;

    public InstitutionCredentialsController(IssuerService issuerService) =>
        _issuerService = issuerService;

    /// <summary>Lists credentials issued by an institution.</summary>
    [HttpGet("{institutionId:guid}/credentials")]
    [ProducesResponseType(typeof(IReadOnlyList<CredentialSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<CredentialSummary>>> ListCredentials(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        var result = await _issuerService.ListInstitutionCredentialsAsync(institutionId, cancellationToken);
        return result switch
        {
            IssuerSuccess<IReadOnlyList<CredentialSummary>> success => Ok(success.Value),
            IssuerFailureResult<IReadOnlyList<CredentialSummary>> failure => throw new IssuerFailureException(failure.Failure),
            _ => throw new InvalidOperationException("Unexpected issuer result.")
        };
    }
}
