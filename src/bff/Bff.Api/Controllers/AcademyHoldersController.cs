using Bff.Api.Models;
using Microsoft.AspNetCore.Mvc;
using SovereignID.Bff.Clients;

namespace Bff.Api.Controllers;

[ApiController]
[Route("academy/holders/me")]
[Produces("application/json")]
public sealed class AcademyHoldersController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetMe(CancellationToken cancellationToken) =>
        SendAcademyAsync(
            new HttpRequestMessage(HttpMethod.Get, "academy/holders/me"),
            cancellationToken);

    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> UpdateProfile(
        [FromBody] UpdateHolderProfileRequest request,
        CancellationToken cancellationToken) =>
        SendAcademyAsync(
            new HttpRequestMessage(HttpMethod.Put, "academy/holders/me/profile")
            {
                Content = JsonContent.Create(request)
            },
            cancellationToken);

    private async Task<IActionResult> SendAcademyAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient(DependencyInjection.AcademyDirectHttpClientName);
        using var response = await httpClient.SendAsync(request, cancellationToken);

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
