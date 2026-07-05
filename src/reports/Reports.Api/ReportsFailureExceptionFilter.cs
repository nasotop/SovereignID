using Reports.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Reports.Api;

public sealed class ReportsFailureExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ReportsFailureException reportsFailure)
        {
            return;
        }

        var failure = reportsFailure.Failure;
        context.Result = new ObjectResult(new ProblemDetails
        {
            Title = "Reports operation failed",
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
