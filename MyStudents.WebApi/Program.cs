using MyStudents.Application;
using MyStudents.Infrastructure;
using Scalar.AspNetCore;
using MyStudents.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi(); // Built-in OpenAPI .NET 10

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
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

app.UseCors("AllowAll"); // Move to top

app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
// Enable API documentation in all environments for better debugging during staging
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("MyStudents API")
           .WithTheme(ScalarTheme.Moon)
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
