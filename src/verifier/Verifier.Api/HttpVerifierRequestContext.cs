using System.Net;
using Verifier.Application;

namespace Verifier.Api;

/// <summary>Resuelve los metadatos del verificador desde el <see cref="HttpContext"/> actual.</summary>
internal sealed class HttpVerifierRequestContext : IVerifierRequestContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpVerifierRequestContext(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public IPAddress? ClientIp => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;

    public string? UserAgent
    {
        get
        {
            var userAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
            return string.IsNullOrWhiteSpace(userAgent) ? null : userAgent;
        }
    }
}
