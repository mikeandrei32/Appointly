using Appointly.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Appointly.Api.Configuration;

public sealed class BookingExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not BookingException exception) return;
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = exception.StatusCode, Title = exception.Message
        }) { StatusCode = exception.StatusCode };
        context.ExceptionHandled = true;
    }
}
