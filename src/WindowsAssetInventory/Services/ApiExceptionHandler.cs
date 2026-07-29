using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WindowsAssetInventory.Services;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    private static readonly Action<ILogger, Exception?> LogUnhandledException =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1001, nameof(LogUnhandledException)),
            "Unhandled request exception");

    private static readonly Action<ILogger, int, Exception?> LogRejectedRequest =
        LoggerMessage.Define<int>(
            LogLevel.Warning,
            new EventId(1002, nameof(LogRejectedRequest)),
            "Request rejected with status {StatusCode}");

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            DomainConflictException => (
                StatusCodes.Status409Conflict,
                "Business rule conflict",
                exception.Message),
            InvalidDataException => (
                StatusCodes.Status400BadRequest,
                "Invalid input data",
                exception.Message),
            DbUpdateException => (
                StatusCodes.Status409Conflict,
                "Database constraint conflict",
                "The operation conflicts with an existing asset or user."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected server error",
                "An unexpected error occurred.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, exception);
        }
        else
        {
            LogRejectedRequest(logger, status, exception);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            }
        });
    }
}
