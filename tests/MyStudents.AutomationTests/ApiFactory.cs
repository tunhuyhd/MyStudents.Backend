using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyStudents.Application.Auth.Dto;
using MyStudents.Infrastructure.Auth;
using MyStudents.Infrastructure.Persistence.Context;

namespace MyStudents.AutomationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public const string Password = "Test-only-password!42";
    public static readonly Guid AdminId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid AdminRole = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid UserRole = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public ApiFactory() => connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureLogging(logging => logging.ClearProviders().AddConsole());
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Key"] = "isolated-test-signing-key-at-least-32-bytes-long-only-for-tests",
            ["JwtSettings:Issuer"] = "MyStudents.Tests",
            ["JwtSettings:Audience"] = "MyStudents.Tests",
            ["JwtSettings:ExpiryMinutes"] = "15",
            ["CloudinarySettings:UseLocalStorage"] = "true"
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        });
    }

    public async Task<HttpClient> InitializeAsync(bool bootstrap = true)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreatedAsync();
        if (bootstrap)
            await scope.ServiceProvider.GetRequiredService<AdminBootstrapper>().InitializeAsync(Password);
        return client;
    }

    public async Task WithDbAsync(Func<ApplicationDbContext, Task> action)
    {
        await using var scope = Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    public static async Task<AuthResponse> RegisterAsync(HttpClient client)
    {
        var username = "teacher-" + Guid.NewGuid().ToString("N");
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(username, username + "@example.test", Password, "Test Teacher"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public static async Task<AuthResponse> AdminLoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/admin/login", new LoginRequest("admin", Password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public static void Authenticate(HttpClient client, AuthResponse auth) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }
}

