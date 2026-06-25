using Auth.Api.Models;
using Auth.Application;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IssueNonceUseCase _issueNonceUseCase;
    private readonly VerifySiweUseCase _verifySiweUseCase;

    public AuthController(IssueNonceUseCase issueNonceUseCase, VerifySiweUseCase verifySiweUseCase)
    {
        _issueNonceUseCase = issueNonceUseCase;
        _verifySiweUseCase = verifySiweUseCase;
    }

    /// <summary>Emite un auth challenge (nonce de un solo uso, TTL 600 s).</summary>
    /// <remarks>El nonce devuelto debe incluirse en el mensaje SIWE que se firma y envía a <c>POST /auth/verify</c>.</remarks>
    [HttpGet("nonce")]
    [ProducesResponseType(typeof(NonceResponse), StatusCodes.Status200OK)]
    public ActionResult<NonceResponse> GetNonce()
    {
        var result = _issueNonceUseCase.Execute();
        return Ok(new NonceResponse(result.Nonce, result.ExpiresAt));
    }

    /// <summary>Verifica un mensaje SIWE firmado y, si es válido, emite un JWT de sesión (TTL 24 h).</summary>
    /// <remarks>Errores de negocio (RFC 7807 Problem Details, extensión <c>error</c>): <c>siwe_parse_failed</c> y <c>unsupported_chain</c> (400); <c>nonce_unknown</c>, <c>nonce_expired</c>, <c>nonce_consumed</c> y <c>signature_mismatch</c> (401).</remarks>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(VerifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<VerifyResponse> Verify([FromBody] VerifyRequest request)
    {
        var result = _verifySiweUseCase.Execute(request.Message, request.Signature);

        return result switch
        {
            VerifySiweSuccess success => Ok(new VerifyResponse(
                success.Jwt,
                success.Address,
                success.ExpiresAt)),
            VerifySiweFailure failure => throw new AuthFailureException(failure.Failure),
            _ => throw new InvalidOperationException("Unexpected verify result.")
        };
    }
}
