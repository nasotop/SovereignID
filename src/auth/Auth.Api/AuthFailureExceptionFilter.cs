using Auth.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Auth.Api;

public sealed class AuthFailureException(AuthFailure failure) : Exception(failure.Detail)
{
    public AuthFailure Failure { get; } = failure;
}

public sealed class AuthFailureExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not AuthFailureException authFailure)
        {
            return;
        }

        var failure = authFailure.Failure;
        context.Result = new ObjectResult(new ProblemDetails
        {
            Title = "Authentication failed",
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
