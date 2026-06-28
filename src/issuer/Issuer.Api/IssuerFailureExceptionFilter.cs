using Issuer.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Issuer.Api;

public sealed class IssuerFailureException(IssuerFailure failure) : Exception(failure.Detail)
{
    public IssuerFailure Failure { get; } = failure;
}

public sealed class IssuerFailureExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not IssuerFailureException issuerFailure)
        {
            return;
        }

        var failure = issuerFailure.Failure;
        context.Result = new ObjectResult(new ProblemDetails
        {
            Title = "Issuer operation failed",
            Status = failure.StatusCode,
            Detail = failure.Detail,
            Extensions = { ["error"] = failure.ErrorCode }
        })
        {
            StatusCode = failure.StatusCode
        };

        context.ExceptionHandled = true;
    }
}
