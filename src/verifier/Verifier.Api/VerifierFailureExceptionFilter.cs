using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Verifier.Api;

/// <summary>Error de entrada/protocolo del verifier, mapeado a Problem Details con extensión <c>error</c>.</summary>
public sealed class VerifierFailureException(int statusCode, string errorCode, string detail, string title)
    : Exception(detail)
{
    public int StatusCode { get; } = statusCode;

    public string ErrorCode { get; } = errorCode;

    public string Title { get; } = title;
}

public sealed class VerifierFailureExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not VerifierFailureException failure)
        {
            return;
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Title = failure.Title,
            Status = failure.StatusCode,
            Detail = failure.Message,
            Extensions = { ["error"] = failure.ErrorCode }
        })
        {
            StatusCode = failure.StatusCode
        };

        context.ExceptionHandled = true;
    }
}
