using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Auth.Dto;
using MyStudents.Domain.Entities.Enum;

namespace MyStudents.AutomationTests;

public class SecurityApiTests : IAsyncLifetime
{
    private readonly ApiFactory factory = new();
    private HttpClient client = null!;
    public async Task InitializeAsync() => client = await factory.InitializeAsync();
    public Task DisposeAsync() { client.Dispose(); factory.Dispose(); return Task.CompletedTask; }

    private Task<HttpResponseMessage> Refresh(AuthResponse auth) => client.PostAsJsonAsync("/api/v1/auth/refresh-token", new RefreshTokenRequest(auth.Token, auth.RefreshToken));
    private Task<HttpResponseMessage> SetStatus(Guid id, Status status) => client.PatchAsJsonAsync($"/api/v1/admin/users/{id}/status", new { userId = id, newStatus = status });
    private Task<HttpResponseMessage> SetRole(Guid id, Guid role) => client.PutAsJsonAsync($"/api/v1/admin/users/{id}/role", role);

    [Theory]
    [InlineData(false, HttpStatusCode.Unauthorized)]
    [InlineData(true, HttpStatusCode.Forbidden)]
    public async Task AdminEndpointsRejectAnonymousAndRegularUsers(bool authenticated, HttpStatusCode expected)
    {
        var user = await ApiFactory.RegisterAsync(client);
        if (authenticated) ApiFactory.Authenticate(client, user);
        Assert.Equal(expected, (await client.GetAsync("/api/v1/admin/roles")).StatusCode);
        Assert.Equal(expected, (await client.GetAsync("/api/v1/admin/users")).StatusCode);
        Assert.Equal(expected, (await SetRole(user.UserId, ApiFactory.AdminRole)).StatusCode);
        Assert.Equal(expected, (await SetStatus(user.UserId, Status.Inactive)).StatusCode);
        await factory.WithDbAsync(async db =>
        {
            var unchanged = await db.Users.SingleAsync(u => u.Id == user.UserId);
            Assert.Equal(ApiFactory.UserRole, unchanged.RoleId);
            Assert.Equal(Status.Active, unchanged.Status);
        });
    }

    [Fact]
    public async Task AdminCanReadAndUpdateAccountsWithAuditFields()
    {
        var user = await ApiFactory.RegisterAsync(client);
        ApiFactory.Authenticate(client, await ApiFactory.AdminLoginAsync(client));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/roles")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SetRole(user.UserId, ApiFactory.AdminRole)).StatusCode);
        await factory.WithDbAsync(async db =>
        {
            var changed = await db.Users.SingleAsync(u => u.Id == user.UserId);
            Assert.Equal(ApiFactory.AdminRole, changed.RoleId);
            Assert.Equal(ApiFactory.AdminId, changed.LastModifiedBy);
            Assert.NotNull(changed.LastModifiedOn);
            Assert.Null(changed.RefreshToken);
        });
    }

    [Fact]
    public async Task AdminCannotDisableOrDemoteSelfAndRejectsInvalidInputs()
    {
        ApiFactory.Authenticate(client, await ApiFactory.AdminLoginAsync(client));
        Assert.Equal(HttpStatusCode.BadRequest, (await SetStatus(ApiFactory.AdminId, Status.Inactive)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SetRole(ApiFactory.AdminId, ApiFactory.UserRole)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SetRole(ApiFactory.AdminId, Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SetStatus(ApiFactory.AdminId, (Status)999)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/users")).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LockingAccountImmediatelyRevokesAccessAndRefreshAndPreventsLogin(bool administrator)
    {
        var user = await ApiFactory.RegisterAsync(client);
        var admin = await ApiFactory.AdminLoginAsync(client);
        if (administrator)
        {
            ApiFactory.Authenticate(client, admin);
            (await SetRole(user.UserId, ApiFactory.AdminRole)).EnsureSuccessStatusCode();
            var login = await client.PostAsJsonAsync("/api/v1/admin/login", new LoginRequest(user.Username, ApiFactory.Password));
            login.EnsureSuccessStatusCode();
            user = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
        }
        ApiFactory.Authenticate(client, user);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        ApiFactory.Authenticate(client, admin);
        (await SetStatus(user.UserId, Status.Inactive)).EnsureSuccessStatusCode();
        ApiFactory.Authenticate(client, user);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(user)).StatusCode);
        var endpoint = administrator ? "/api/v1/admin/login" : "/api/v1/auth/login";
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(endpoint, new LoginRequest(user.Username, ApiFactory.Password))).StatusCode);
        ApiFactory.Authenticate(client, admin);
        (await SetStatus(user.UserId, Status.Active)).EnsureSuccessStatusCode();
        ApiFactory.Authenticate(client, user);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(endpoint, new LoginRequest(user.Username, ApiFactory.Password))).StatusCode);
    }

    [Fact]
    public async Task RoleChangeRevokesOldTokensAndNewLoginReceivesNewRole()
    {
        var user = await ApiFactory.RegisterAsync(client);
        ApiFactory.Authenticate(client, await ApiFactory.AdminLoginAsync(client));
        (await SetRole(user.UserId, ApiFactory.AdminRole)).EnsureSuccessStatusCode();
        ApiFactory.Authenticate(client, user);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(user)).StatusCode);
        var response = await client.PostAsJsonAsync("/api/v1/admin/login", new LoginRequest(user.Username, ApiFactory.Password));
        response.EnsureSuccessStatusCode();
        var promoted = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        ApiFactory.Authenticate(client, promoted);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/users")).StatusCode);
    }

    [Fact]
    public async Task LogoutRevokesAccessAndRefreshTokens()
    {
        var user = await ApiFactory.RegisterAsync(client);
        ApiFactory.Authenticate(client, user);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(user)).StatusCode);
    }

    [Fact]
    public async Task PasswordChangeRevokesSessionsAndRequiresNewPassword()
    {
        var user = await ApiFactory.RegisterAsync(client);
        ApiFactory.Authenticate(client, user);
        var change = await client.PutAsJsonAsync("/api/v1/auth/change-password", new { oldPassword = ApiFactory.Password, newPassword = "New-test-password!42" });
        change.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(user)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(user.Username, ApiFactory.Password))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(user.Username, "New-test-password!42"))).StatusCode);
    }

    [Theory]
    [InlineData("incorrect-password", "New-test-password!42")]
    [InlineData(ApiFactory.Password, "short")]
    public async Task InvalidPasswordChangeDoesNotRevokeSession(string oldPassword, string newPassword)
    {
        var user = await ApiFactory.RegisterAsync(client);
        ApiFactory.Authenticate(client, user);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/v1/auth/change-password", new { oldPassword, newPassword })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task RefreshRotatesTokenAndRejectsReuse()
    {
        var user = await ApiFactory.RegisterAsync(client);
        var response = await Refresh(user);
        response.EnsureSuccessStatusCode();
        var refreshed = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.NotEqual(user.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(user)).StatusCode);
        ApiFactory.Authenticate(client, refreshed);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-token")]
    public async Task RefreshRejectsMissingOrUnknownToken(string? token)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh-token", new { accessToken = "unused", refreshToken = token });
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RefreshRejectsExpiredOrMissingExpiry(bool missingExpiry)
    {
        var user = await ApiFactory.RegisterAsync(client);
        await factory.WithDbAsync(async db =>
        {
            var entity = await db.Users.SingleAsync(u => u.Id == user.UserId);
            entity.RefreshTokenExpiryTime = missingExpiry ? null : DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(user)).StatusCode);
    }
}
