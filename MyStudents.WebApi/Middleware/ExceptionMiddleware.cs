using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Exceptions;
using Npgsql;

namespace MyStudents.WebApi.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
        catch (Exception ex)
        {
            var (status, error) = ex switch
            {
                ValidationException validation => (400, new ApiError("ERR_VALIDATION", "Please check the submitted fields.", validation.Errors)),
                BusinessException business => (400, new ApiError(business.ErrorCode, business.Message, Parameters: business.Parameters)),
                ConflictException conflict => (409, new ApiError(conflict.ErrorCode, "The request conflicts with existing data.")),
                NotFoundException => (404, new ApiError("ERR_NOT_FOUND", "The requested resource was not found.")),
                UnauthorizedAccessException => (401, new ApiError("ERR_UNAUTHORIZED", "Authentication failed or the session is no longer valid.")),
                ForbiddenException => (403, new ApiError("ERR_FORBIDDEN", "You do not have permission to perform this action.")),
                DbUpdateConcurrencyException => (409, new ApiError("ERR_CONCURRENT_UPDATE", "The data changed. Reload and try again.")),
                _ when PostgreSqlState(ex) is "40001" or "40P01" => (409, new ApiError("ERR_CONCURRENT_UPDATE", "The data changed. Reload and try again.")),
                _ when PostgreSqlState(ex) == "23505" => (409, new ApiError("ERR_DUPLICATE", "A matching record already exists.")),
                _ when PostgreSqlState(ex) == "23503" => (409, new ApiError("ERR_REFERENCE_CONFLICT", "A related record does not exist or is still in use.")),
                _ => (500, new ApiError("ERR_INTERNAL", "An unexpected error occurred. Please try again later."))
            };
            if (status == 500) logger.LogError(ex, "Unhandled request failure");
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(error with { TraceId = context.TraceIdentifier }, context.RequestAborted);
        }
    }

    private static string? PostgreSqlState(Exception ex)
        => (ex as PostgresException ?? ex.InnerException as PostgresException)?.SqlState;
}

