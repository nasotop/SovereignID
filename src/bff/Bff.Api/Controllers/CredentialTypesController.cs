using Bff.Api.Models;
using Microsoft.AspNetCore.Mvc;
using SovereignID.Bff.Clients;

namespace Bff.Api.Controllers;

[ApiController]
[Route("issuer/credential-types")]
[Produces("application/json")]
public sealed class CredentialTypesController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CredentialTypeSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListCredentialTypes(CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient(DependencyInjection.IssuerDirectHttpClientName);
        using var response = await httpClient.GetAsync("issuer/credential-types", cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = contentType,
        };
    }
}
