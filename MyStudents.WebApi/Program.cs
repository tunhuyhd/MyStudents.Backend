using MyStudents.Application;
using MyStudents.Infrastructure;
using Scalar.AspNetCore;
using MyStudents.WebApi.Middleware;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(new ApiError(
        "ERR_VALIDATION", "Please check the submitted fields.",
        context.ModelState.Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(entry => entry.Key, entry => entry.Value!.Errors.Select(_ => "Invalid or missing value.").ToArray()),
        TraceId: context.HttpContext.TraceIdentifier));
});
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("password-reset-email", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 5, Window = TimeSpan.FromMinutes(15), QueueLimit = 0 }));
    options.AddPolicy("password-reset-token", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 20, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 }));
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        await context.HttpContext.Response.WriteAsJsonAsync(new ApiError("ERR_RATE_LIMIT", "Too many requests. Please try again later."), token);
    };
});
builder.Services.AddOpenApi(); // Built-in OpenAPI .NET 10

// Only allow explicitly configured frontend origins. Development keeps the local Next.js origin usable.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
if (builder.Environment.IsDevelopment() && allowedOrigins.Length == 0)
    allowedOrigins = ["http://localhost:3000", "https://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (args.Contains("--bootstrap-admin"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var password = Environment.GetEnvironmentVariable("MYSTUDENTS_BOOTSTRAP_ADMIN_PASSWORD") ?? string.Empty;
    await scope.ServiceProvider.GetRequiredService<MyStudents.Infrastructure.Auth.AdminBootstrapper>()
        .InitializeAsync(password);
    app.Logger.LogInformation("Administrator initialized. Remove the bootstrap password from the environment.");
    return;
}

app.UseCors("Frontend");

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .AllowAnonymous();

app.UseMiddleware<ExceptionMiddleware>();
app.UseStatusCodePages(async statusContext =>
{
    var response = statusContext.HttpContext.Response;
    var error = response.StatusCode switch
    {
        401 => new ApiError("ERR_UNAUTHORIZED", "Authentication is required or the session has expired."),
        403 => new ApiError("ERR_FORBIDDEN", "You do not have permission to perform this action."),
        404 => new ApiError("ERR_NOT_FOUND", "The requested resource was not found."),
        _ => new ApiError("ERR_REQUEST", "The request could not be completed.")
    };
    await response.WriteAsJsonAsync(error with { TraceId = statusContext.HttpContext.TraceIdentifier });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("ApiDocs:Enabled"))
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("MyStudents API")
               .WithTheme(ScalarTheme.Moon)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();

public partial class Program { }
