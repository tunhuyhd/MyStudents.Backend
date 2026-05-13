# Hướng Dẫn Infrastructure Layer

## Mục Đích

**Infrastructure Layer** là implementation layer, chứa:

- Database access (Entity Framework Core)
- Repository implementations
- External services (Email, File Storage, Authentication)
- Infrastructure services
- Database migrations
- Data seeding

**Nguyên tắc**: Infrastructure Layer phụ thuộc vào **Domain** và **Application** layers, nhưng implement các interfaces được định nghĩa ở những layers đó.

## Cấu Trúc Thư Mục

```
{{ProjectName}}.Infrastructure/
├── {{ProjectName}}.Infrastructure.csproj
├── Startup.cs                          # Service registration
├── Auth/                               # Authentication services
│   ├── CurrentUser.cs
│   ├── CurrentUserService.cs
│   ├── TokenService.cs
│   ├── GoogleAuthService.cs
│   ├── JwtSettings.cs
│   └── GoogleSettings.cs
├── Persistence/                        # Data access
│   ├── Context/
│   │   ├── BaseDbContext.cs
│   │   └── ApplicationDbContext.cs    # EF Core DbContext
│   ├── Repositories/
│   │   └── ApplicationDbRepository.cs # Generic repository
│   ├── Configuration/                  # Entity configurations
│   │   ├── UserConfiguration.cs
│   │   └── ProjectConfiguration.cs
│   ├── Interceptors/                   # EF Interceptors
│   │   └── DispatchDomainEventsInterceptor.cs
│   ├── Migrations/                     # EF Migrations
│   └── Seeder/                        # Data seeding
│       └── ApplicationDbSeeder.cs
└── Services/                           # External services
    ├── EmailService.cs
    ├── FileStorageService.cs
    └── Configuration/
        └── FileStorageSettings.cs
```

## Dependencies

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference Domain và Application -->
    <ProjectReference Include="..\{{ProjectName}}.Domain\{{ProjectName}}.Domain.csproj" />
    <ProjectReference Include="..\{{ProjectName}}.Application\{{ProjectName}}.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Entity Framework Core -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.2" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
    
    <!-- JWT Authentication -->
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.3" />
    
    <!-- Google OAuth -->
    <PackageReference Include="Google.Apis.Auth" Version="1.68.0" />
    
    <!-- File Storage -->
    <PackageReference Include="CloudinaryDotNet" Version="1.28.0" />
    
    <!-- MediatR -->
    <PackageReference Include="MediatR" Version="14.0.0" />
  </ItemGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
</Project>
```

## Service Registration (Startup.cs)

```csharp
// Startup.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using {{ProjectName}}.Application.Common;
using {{ProjectName}}.Application.Common.Authorization;
using {{ProjectName}}.Application.Identity;
using {{ProjectName}}.Domain.Common;
using {{ProjectName}}.Infrastructure.Persistence.Context;
using {{ProjectName}}.Infrastructure.Persistence.Repositories;
using {{ProjectName}}.Infrastructure.Auth;
using {{ProjectName}}.Infrastructure.Services;

namespace {{ProjectName}}.Infrastructure;

public static class Startup
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddAuth();
        services.AddPersistence(configuration);
        services.AddExternalServices(configuration);

        return services;
    }

    private static IServiceCollection AddAuth(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        
        return services;
    }

    private static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Register interceptors
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        // Register DbContext
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(Startup).Assembly.GetName().Name));
        });

        // Register IApplicationDbContext
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Register repositories
        services.AddScoped(typeof(IRepository<>), typeof(ApplicationDbRepository<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(ApplicationDbRepository<>));

        return services;
    }

    private static IServiceCollection AddExternalServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure settings
        services.Configure<FileStorageSettings>(
            configuration.GetSection("FileStorage"));

        // Register services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFileStorageService, FileStorageService>();

        return services;
    }
}
```

## Entity Framework Core Implementation

### 1. ApplicationDbContext

```csharp
// Persistence/Context/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using {{ProjectName}}.Application.Common;
using {{ProjectName}}.Application.Common.Authorization;
using {{ProjectName}}.Domain.Entities;

namespace {{ProjectName}}.Infrastructure.Persistence.Context;

public class ApplicationDbContext : BaseDbContext, IApplicationDbContext
{
    public ApplicationDbContext(
        DbContextOptions options, 
        ICurrentUser currentUser) 
        : base(options, currentUser)
    {
    }

    // DbSets
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectRole> ProjectRoles => Set<ProjectRole>();
    public DbSet<ProjectPermission> ProjectPermissions => Set<ProjectPermission>();
    public DbSet<UserProject> UserProjects => Set<UserProject>();
    public DbSet<ProjectRolePermission> ProjectRolePermissions => Set<ProjectRolePermission>();
    public DbSet<InvitationJoiningProject> Invitations => Set<InvitationJoiningProject>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);

        // Manual configurations (nếu cần)
        ConfigureUser(modelBuilder);
        ConfigureUserProject(modelBuilder);
    }

    private void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }

    private void ConfigureUserProject(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProject>(entity =>
        {
            entity.HasKey(up => new { up.UserId, up.ProjectId });

            entity.HasOne(up => up.User)
                .WithMany(u => u.UserProjects)
                .HasForeignKey(up => up.UserId);

            entity.HasOne(up => up.Project)
                .WithMany(p => p.UserProjects)
                .HasForeignKey(up => up.ProjectId);
        });
    }
}
```

### 2. BaseDbContext

```csharp
// Persistence/Context/BaseDbContext.cs
using Microsoft.EntityFrameworkCore;
using {{ProjectName}}.Application.Common.Authorization;
using {{ProjectName}}.Domain.Common;

namespace {{ProjectName}}.Infrastructure.Persistence.Context;

public abstract class BaseDbContext : DbContext
{
    private readonly ICurrentUser _currentUser;

    protected BaseDbContext(
        DbContextOptions options, 
        ICurrentUser currentUser) 
        : base(options)
    {
        _currentUser = currentUser;
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        // Update audit fields
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            var userId = _currentUser.IsAuthenticated() 
                ? _currentUser.GetUserId() 
                : Guid.Empty;

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedBy = userId;
                    entry.Entity.LastModifiedOn = DateTime.UtcNow;
                    break;

                case EntityState.Deleted:
                    if (entry.Entity is ISoftDelete softDelete)
                    {
                        // Soft delete
                        entry.State = EntityState.Modified;
                        softDelete.DeletedOn = DateTime.UtcNow;
                        softDelete.DeletedBy = userId;
                    }
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(GenerateSoftDeleteFilter(entityType.ClrType));
            }
        }
    }

    private static LambdaExpression GenerateSoftDeleteFilter(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, nameof(ISoftDelete.DeletedOn));
        var condition = Expression.Equal(property, Expression.Constant(null, typeof(DateTime?)));
        return Expression.Lambda(condition, parameter);
    }
}
```

### 3. Repository Implementation

```csharp
// Persistence/Repositories/ApplicationDbRepository.cs
using Microsoft.EntityFrameworkCore;
using {{ProjectName}}.Domain.Common;
using {{ProjectName}}.Infrastructure.Persistence.Context;
using System.Linq.Expressions;

namespace {{ProjectName}}.Infrastructure.Persistence.Repositories;

public class ApplicationDbRepository<T> : IRepository<T>
    where T : Entity, IAggregateRoot
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public ApplicationDbRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    // Read operations
    public async Task<T?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<List<T>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public async Task<List<T>> ListAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(predicate, cancellationToken);
    }

    public async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    // Write operations
    public async Task<T> AddAsync(
        T entity, 
        CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task<IEnumerable<T>> AddRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
        return entities;
    }

    public Task UpdateAsync(
        T entity, 
        CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task UpdateRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        _dbSet.UpdateRange(entities);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        T entity, 
        CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        _dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
```

### 4. Domain Events Interceptor

```csharp
// Persistence/Interceptors/DispatchDomainEventsInterceptor.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using {{ProjectName}}.Domain.Common;

namespace {{ProjectName}}.Infrastructure.Persistence.Interceptors;

public class DispatchDomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IMediator _mediator;

    public DispatchDomainEventsInterceptor(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await DispatchDomainEvents(eventData.Context, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEvents(
        DbContext context,
        CancellationToken cancellationToken)
    {
        var entities = context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entities.ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }
}
```

## Authentication Services

### 1. CurrentUser Service

```csharp
// Auth/CurrentUser.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using {{ProjectName}}.Application.Common.Authorization;

namespace {{ProjectName}}.Infrastructure.Auth;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        return userId;
    }

    public string GetUsername()
    {
        var username = _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.Name);

        return username ?? throw new UnauthorizedAccessException("User is not authenticated");
    }

    public string GetUserEmail()
    {
        var email = _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.Email);

        return email ?? throw new UnauthorizedAccessException("User is not authenticated");
    }

    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }
}
```

### 2. JWT Token Service

```csharp
// Auth/TokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using {{ProjectName}}.Application.Identity;
using {{ProjectName}}.Domain.Entities;

namespace {{ProjectName}}.Infrastructure.Auth;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// Auth/JwtSettings.cs
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; }
}
```

## Migrations

### Create Migration

```bash
# From Infrastructure project
dotnet ef migrations add InitialCreate --startup-project ../{{ProjectName}}.WebApi

# Or from solution root
dotnet ef migrations add InitialCreate --project {{ProjectName}}.Infrastructure --startup-project {{ProjectName}}.WebApi
```

### Update Database

```bash
dotnet ef database update --startup-project ../{{ProjectName}}.WebApi
```

### Remove Last Migration

```bash
dotnet ef migrations remove --startup-project ../{{ProjectName}}.WebApi
```

## Data Seeding

```csharp
// Persistence/Seeder/ApplicationDbSeeder.cs
using {{ProjectName}}.Domain.Entities;
using {{ProjectName}}.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace {{ProjectName}}.Infrastructure.Persistence.Seeder;

public static class ApplicationDbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Seed ProjectRoles
        if (!await context.ProjectRoles.AnyAsync())
        {
            var roles = new List<ProjectRole>
            {
                new() { Code = "PROJECT_ADMIN", Name = "Project Admin" },
                new() { Code = "PROJECT_MEMBER", Name = "Project Member" },
                new() { Code = "PROJECT_VIEWER", Name = "Project Viewer" }
            };

            await context.ProjectRoles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
        }

        // Seed Permissions
        if (!await context.ProjectPermissions.AnyAsync())
        {
            var permissions = new List<ProjectPermission>
            {
                new() { Code = "PROJECT_EDIT", Name = "Edit Project" },
                new() { Code = "PROJECT_DELETE", Name = "Delete Project" },
                new() { Code = "PROJECT_VIEW", Name = "View Project" }
            };

            await context.ProjectPermissions.AddRangeAsync(permissions);
            await context.SaveChangesAsync();
        }
    }
}
```

## External Services

### Email Service

```csharp
// Services/EmailService.cs
using {{ProjectName}}.Application.Common.Interfaces;

namespace {{ProjectName}}.Infrastructure.Services;

public class EmailService : IEmailService
{
    public async Task SendEmailAsync(
        string to, 
        string subject, 
        string body)
    {
        // Implement email sending logic
        // Using SMTP, SendGrid, etc.
        await Task.CompletedTask;
    }
}
```

## Best Practices

### 1. DbContext

- ✅ One DbContext per application
- ✅ Register as Scoped
- ✅ Use interceptors cho cross-cutting concerns
- ✅ Apply global query filters
- ❌ KHÔNG hardcode connection string

### 2. Migrations

- ✅ Meaningful migration names
- ✅ Review generated migrations
- ✅ Include data seeding
- ✅ Version control migrations
- ❌ KHÔNG modify existing migrations sau khi deploy

### 3. Repository

- ✅ Generic repository cho common operations
- ✅ Specific repositories nếu cần complex queries
- ✅ Async methods
- ❌ KHÔNG expose IQueryable

### 4. External Services

- ✅ Configuration via Options pattern
- ✅ Interface trong Application, implementation trong Infrastructure
- ✅ Error handling và retry logic
- ✅ Logging

## Checklist

- [ ] Create project `{{ProjectName}}.Infrastructure`
- [ ] Install EF Core và provider packages
- [ ] Create DbContext và BaseDbContext
- [ ] Implement IApplicationDbContext
- [ ] Create Repository implementation
- [ ] Add EF Interceptors
- [ ] Configure entity relationships
- [ ] Create initial migration
- [ ] Implement authentication services
- [ ] Add external services
- [ ] Create data seeder
- [ ] Configure Startup.cs
- [ ] Test database connection

---

**Next**: [WebAPI Layer Guide](./WEBAPI_LAYER_GUIDE.md)
