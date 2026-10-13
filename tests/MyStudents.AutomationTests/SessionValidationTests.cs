using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MyStudents.Application.Admin.Commands;
using MyStudents.Application.Common.Exceptions;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities.Enum;
using MyStudents.Infrastructure.Persistence.Context;

namespace MyStudents.AutomationTests;

public class SessionValidationTests
{
    [Theory]
    [InlineData("missing-stamp")]
    [InlineData("wrong-stamp")]
    [InlineData("invalid-signature")]
    [InlineData("expired")]
    public async Task InvalidAndLegacyAccessTokensAreRejected(string variant)
    {
        using var factory = new ApiFactory();
        using var client = await factory.InitializeAsync();
        var user = await ApiFactory.RegisterAsync(client);
        Guid stamp = default;
        await factory.WithDbAsync(async db => stamp = (await db.Users.SingleAsync(u => u.Id == user.UserId)).SecurityStamp);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Role, "User")
        };
        if (variant != "missing-stamp") claims.Add(new Claim("security_stamp", (variant == "wrong-stamp" ? Guid.NewGuid() : stamp).ToString()));
        var key = variant == "invalid-signature" ? "different-test-signing-key-at-least-32-bytes-long-for-tests" : "isolated-test-signing-key-at-least-32-bytes-long-only-for-tests";
        var token = new JwtSecurityToken("MyStudents.Tests", "MyStudents.Tests", claims,
            expires: variant == "expired" ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task ApplicationHandlersAlsoEnforceAdministratorPermission()
    {
        using var factory = new ApiFactory();
        using var client = await factory.InitializeAsync();
        var user = await ApiFactory.RegisterAsync(client);
        await factory.WithDbAsync(async db =>
        {
            var identity = new TestUser(user.UserId);
            await Assert.ThrowsAsync<ForbiddenException>(() => new UpdateUserRoleCommandHandler(db, identity)
                .Handle(new UpdateUserRoleCommand(user.UserId, ApiFactory.AdminRole), default));
            await Assert.ThrowsAsync<ForbiddenException>(() => new UpdateUserStatusCommandHandler(db, identity)
                .Handle(new UpdateUserStatusCommand(ApiFactory.AdminId, Status.Inactive), default));
        });
    }

    [Fact]
    public async Task ConcurrentRefreshCannotOverwriteRevokedSession()
    {
        using var factory = new ApiFactory();
        using var client = await factory.InitializeAsync();
        var user = await ApiFactory.RegisterAsync(client);
        await using var staleScope = factory.Services.CreateAsyncScope();
        var staleDb = staleScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var staleUser = await staleDb.Users.SingleAsync(u => u.Id == user.UserId);
        ApiFactory.Authenticate(client, user);
        (await client.PostAsync("/api/v1/auth/logout", null)).EnsureSuccessStatusCode();
        staleUser.RefreshToken = "attempt-to-resurrect-session";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleDb.SaveChangesAsync());
        await factory.WithDbAsync(async db => Assert.Null((await db.Users.SingleAsync(u => u.Id == user.UserId)).RefreshToken));
    }

    private sealed class TestUser(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public string? Role => "User";
        public bool IsAuthenticated => true;
    }
}
