namespace MyStudents.WebApi.Middleware;

public record ApiError(string ErrorCode, string Message,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    IDictionary<string, string>? Parameters = null,
    string? TraceId = null);

