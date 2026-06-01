using Auth.Api.Models;
using Auth.Application;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IssueNonceUseCase _issueNonceUseCase;
    private readonly VerifySiweUseCase _verifySiweUseCase;

    public AuthController(IssueNonceUseCase issueNonceUseCase, VerifySiweUseCase verifySiweUseCase)
    {
        _issueNonceUseCase = issueNonceUseCase;
        _verifySiweUseCase = verifySiweUseCase;
    }

    [HttpGet("nonce")]
    [ProducesResponseType(typeof(NonceResponse), StatusCodes.Status200OK)]
    public ActionResult<NonceResponse> GetNonce()
    {
        var result = _issueNonceUseCase.Execute();
        return Ok(new NonceResponse(result.Nonce, result.ExpiresAt));
    }

    [HttpPost("verify")]
    [ProducesResponseType(typeof(VerifyResponse), StatusCodes.Status200OK)]
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
