using System.Net;
using System.Text.Json;
using FCG.Domain.Shared;
using FCG.Domain.Users.Exceptions;

namespace FCG.API.Middlewares;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            UserNotFoundException => (HttpStatusCode.NotFound, exception.Message),
            RootAdminOperationForbiddenException => (HttpStatusCode.Forbidden, exception.Message),
            UserAlreadyExistsException => (HttpStatusCode.Conflict, exception.Message),
            InvalidCredentialsException => (HttpStatusCode.Unauthorized, exception.Message),
            DomainException => (HttpStatusCode.BadRequest, exception.Message),
            BadHttpRequestException => (HttpStatusCode.BadRequest, "The request body is invalid."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            logger.LogError(exception, "Unhandled exception");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = JsonSerializer.Serialize(new { error = message });
        await context.Response.WriteAsync(response);
    }
}