using System.Security.Claims;
using Issuer.Application;

namespace Issuer.Api;

internal sealed class HttpIssuerRequestContext : IIssuerRequestContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpIssuerRequestContext(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public string? WalletAddress =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue("address")
        ?? _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? SubjectDid =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue("did");
}
