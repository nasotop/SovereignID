using Issuer.Api.Models;
using Issuer.Application;
using Issuer.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Issuer.Api.Controllers;

[ApiController]
[Route("issuer/institutions")]
[Produces("application/json")]
[Authorize(Policy = IssuerAuthorizationPolicy.IssuerPolicyName)]
public sealed class InstitutionCredentialsController : ControllerBase
{
    private readonly IssuerService _issuerService;

    public InstitutionCredentialsController(IssuerService issuerService)
    {
        _issuerService = issuerService;
    }

    /// <summary>Lists credentials issued by an institution.</summary>
    [HttpGet("{institutionId:guid}/credentials")]
    [ProducesResponseType(typeof(IReadOnlyList<CredentialSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<CredentialSummary>>> ListCredentials(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        var result = await _issuerService.ListInstitutionCredentialsAsync(institutionId, cancellationToken);
        return FromResult(result, success => (ActionResult<IReadOnlyList<CredentialSummary>>)Ok(success));
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
