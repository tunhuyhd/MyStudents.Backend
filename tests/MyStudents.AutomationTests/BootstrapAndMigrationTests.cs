using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using MyStudents.Application.Auth.Dto;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities.Enum;
using MyStudents.Infrastructure.Auth;
using MyStudents.Infrastructure.Persistence.Context;
using MyStudents.Infrastructure.Persistence.Migrations;

namespace MyStudents.AutomationTests;

public class BootstrapAndMigrationTests
{
    [Fact]
    public async Task SeededAdministratorCannotLoginUntilExplicitBootstrap()
    {
        using var factory = new ApiFactory();
        using var client = await factory.InitializeAsync(bootstrap: false);
        await factory.WithDbAsync(async db =>
        {
            var admin = await db.Users.SingleAsync(u => u.Id == ApiFactory.AdminId);
            Assert.Equal(Status.Inactive, admin.Status);
            Assert.Empty(admin.PasswordHash);
        });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/admin/login", new LoginRequest("admin", "admin123"))).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AdminBootstrapper>().InitializeAsync(ApiFactory.Password);
        var auth = await ApiFactory.AdminLoginAsync(client);
        ApiFactory.Authenticate(client, auth);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/users")).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("admin123")]
    public async Task BootstrapRejectsWeakPassword(string password)
    {
        using var factory = new ApiFactory();
        using var client = await factory.InitializeAsync(bootstrap: false);
        await using var scope = factory.Services.CreateAsyncScope();
        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<AdminBootstrapper>().InitializeAsync(password));
        await factory.WithDbAsync(async db => Assert.Equal(Status.Inactive, (await db.Users.SingleAsync(u => u.Id == ApiFactory.AdminId)).Status));
    }

    [Fact]
    public async Task BootstrapNeverOverwritesConfiguredAdministrator()
    {
        using var factory = new ApiFactory();
        using var client = await factory.InitializeAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<AdminBootstrapper>().InitializeAsync("Different-password!42"));
        await ApiFactory.AdminLoginAsync(client);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MigrationDisablesLegacyCredentialButPreservesChangedPassword(bool legacy)
    {
        using var factory = new ApiFactory();
        using var client = await factory.InitializeAsync();
        string originalHash = "";
        await factory.WithDbAsync(async db =>
        {
            var admin = await db.Users.SingleAsync(u => u.Id == ApiFactory.AdminId);
            if (legacy) admin.PasswordHash = "$2a$11$4HIlo6ImhI/LAIXHPdIxT.YkGP.2YaLGgUbLKOtU4mkL75hRu/1v2";
            originalHash = admin.PasswordHash;
            admin.RefreshToken = "legacy-session";
            admin.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1);
            await db.SaveChangesAsync();
            // Exercise the exact data migration SQL on a relational database.
            foreach (var operation in new HardenAccountSessions().UpOperations.OfType<SqlOperation>())
                await db.Database.ExecuteSqlRawAsync(operation.Sql);
        });
        await factory.WithDbAsync(async db =>
        {
            var admin = await db.Users.SingleAsync(u => u.Id == ApiFactory.AdminId);
            Assert.Equal(legacy ? Status.Inactive : Status.Active, admin.Status);
            Assert.Equal(legacy ? "" : originalHash, admin.PasswordHash);
            Assert.Null(admin.RefreshToken);
            Assert.Null(admin.RefreshTokenExpiryTime);
        });
        Assert.Empty(new HardenAccountSessions().DownOperations.OfType<UpdateDataOperation>());
    }

    [Fact]
    public void PostgreSqlModelMatchesMigrationSnapshot()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused").Options;
        using var db = new ApplicationDbContext(options, new AnonymousUser());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private sealed class AnonymousUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public string? Role => null;
        public bool IsAuthenticated => false;
    }
}
