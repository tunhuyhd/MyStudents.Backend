using System.Net;
using System.Text.Json;
using MyStudents.Application.Common.Exceptions;

namespace MyStudents.WebApi.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Business Rule Violation: Schedule Conflict Detected.");
            await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled Exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, HttpStatusCode statusCode)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var message = exception.Message;

        object response;
        if (exception is BusinessException bizEx)
        {
            response = new 
            { 
                errorCode = bizEx.ErrorCode, 
                parameters = bizEx.Parameters,
                message = bizEx.Message // Fallback
            };
        }
        else
        {
            var detail = exception.InnerException != null 
                ? $"{exception.Message} | Inner: {exception.InnerException.Message}" 
                : exception.Message;
            response = new { message = detail };
        }

        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }
}
