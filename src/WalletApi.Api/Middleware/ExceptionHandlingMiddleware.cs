using System.Net;
using WalletApi.Domain.Exceptions;

namespace WalletApi.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, message) = Map(ex);

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
            }

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = message });
        }
    }

    private static (HttpStatusCode StatusCode, string Message) Map(Exception ex) => ex switch
    {
        OperationAlreadyExistsException => (HttpStatusCode.Conflict, ex.Message),
        OperationNotFoundException => (HttpStatusCode.NotFound, ex.Message),
        ReceiptConflictException => (HttpStatusCode.Conflict, ex.Message),
        InvalidAmountException => (HttpStatusCode.BadRequest, ex.Message),
        InvalidReceiptStatusException => (HttpStatusCode.BadRequest, ex.Message),
        ArgumentOutOfRangeException => (HttpStatusCode.BadRequest, ex.Message),
        _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
    };
}
