using Issuer.Application;
using Issuer.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Issuer.Api.Controllers;

[ApiController]
[Route("issuer/credential-types")]
[Produces("application/json")]
[Authorize(Policy = IssuerAuthorizationPolicy.IssuerPolicyName)]
public sealed class CredentialTypesController : ControllerBase
{
    private readonly IssuerService _issuerService;

    public CredentialTypesController(IssuerService issuerService) =>
        _issuerService = issuerService;

    /// <summary>Lists active credential types available for issuance.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CredentialTypeSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CredentialTypeSummary>>> ListCredentialTypes(
        CancellationToken cancellationToken)
    {
        var result = await _issuerService.ListCredentialTypesAsync(cancellationToken);
        return result switch
        {
            IssuerSuccess<IReadOnlyList<CredentialTypeSummary>> success => Ok(success.Value),
            IssuerFailureResult<IReadOnlyList<CredentialTypeSummary>> failure => throw new IssuerFailureException(failure.Failure),
            _ => throw new InvalidOperationException("Unexpected issuer result.")
        };
    }
}
