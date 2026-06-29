using Academy.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Academy.Api;

public sealed class AcademyFailureException(AcademyFailure failure) : Exception(failure.Detail)
{
    public AcademyFailure Failure { get; } = failure;
}

public sealed class AcademyFailureExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not AcademyFailureException academyFailure)
        {
            return;
        }

        var failure = academyFailure.Failure;
        context.Result = new ObjectResult(new ProblemDetails
        {
            Title = "Academy operation failed",
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

