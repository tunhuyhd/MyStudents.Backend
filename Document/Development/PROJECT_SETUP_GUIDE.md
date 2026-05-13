# Hướng Dẫn Setup Dự Án Từ Đầu

## Tổng Quan

Hướng dẫn này sẽ giúp bạn tạo một dự án ASP.NET Core hoàn chỉnh với **Clean Architecture** và **CQRS Pattern** từ đầu.

## Prerequisites

- .NET 10 SDK hoặc cao hơn
- Visual Studio 2022 / VS Code / Rider
- PostgreSQL (hoặc SQL Server)
- Git
- Postman hoặc curl (để test API)

## Bước 1: Tạo Solution và Projects

### 1.1. Tạo Solution

```bash
# Tạo thư mục dự án
mkdir {{ProjectName}}
cd {{ProjectName}}

# Tạo solution
dotnet new sln -n {{ProjectName}}
```

### 1.2. Tạo Domain Layer

```bash
# Tạo Domain project (Class Library)
dotnet new classlib -n {{ProjectName}}.Domain -f net10.0

# Add vào solution
dotnet sln add {{ProjectName}}.Domain/{{ProjectName}}.Domain.csproj

# Navigate vào project
cd {{ProjectName}}.Domain

# Add MediatR package
dotnet add package MediatR --version 14.0.0

# Tạo cấu trúc thư mục
mkdir Common
mkdir Entities
mkdir Events

cd ..
```

### 1.3. Tạo Application Layer

```bash
# Tạo Application project (Class Library)
dotnet new classlib -n {{ProjectName}}.Application -f net10.0

# Add vào solution
dotnet sln add {{ProjectName}}.Application/{{ProjectName}}.Application.csproj

# Navigate vào project
cd {{ProjectName}}.Application

# Add project reference
dotnet add reference ../{{ProjectName}}.Domain/{{ProjectName}}.Domain.csproj

# Add packages
dotnet add package MediatR --version 14.0.0
dotnet add package FluentValidation --version 12.1.1
dotnet add package FluentValidation.DependencyInjectionExtensions --version 12.1.1
dotnet add package Microsoft.EntityFrameworkCore --version 10.0.2

# Tạo cấu trúc thư mục
mkdir Common
mkdir Common/Behaviors
mkdir Common/Dto
mkdir Common/Exceptions
mkdir Common/Extensions
mkdir Common/Interfaces
mkdir Common/Response
mkdir Projects
mkdir Projects/Commands
mkdir Projects/Queries
mkdir Users
mkdir Users/Commands
mkdir Users/Queries

cd ..
```

### 1.4. Tạo Infrastructure Layer

```bash
# Tạo Infrastructure project (Class Library)
dotnet new classlib -n {{ProjectName}}.Infrastructure -f net10.0

# Add vào solution
dotnet sln add {{ProjectName}}.Infrastructure/{{ProjectName}}.Infrastructure.csproj

# Navigate vào project
cd {{ProjectName}}.Infrastructure

# Add project references
dotnet add reference ../{{ProjectName}}.Domain/{{ProjectName}}.Domain.csproj
dotnet add reference ../{{ProjectName}}.Application/{{ProjectName}}.Application.csproj

# Add packages
dotnet add package Microsoft.EntityFrameworkCore --version 10.0.2
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.0
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.3
dotnet add package Google.Apis.Auth --version 1.68.0
dotnet add package MediatR --version 14.0.0

# Tạo cấu trúc thư mục
mkdir Auth
mkdir Persistence
mkdir Persistence/Context
mkdir Persistence/Repositories
mkdir Persistence/Configuration
mkdir Persistence/Interceptors
mkdir Persistence/Migrations
mkdir Persistence/Seeder
mkdir Services

cd ..
```

### 1.5. Tạo WebAPI Layer

```bash
# Tạo WebAPI project
dotnet new webapi -n {{ProjectName}}.WebApi -f net10.0

# Add vào solution
dotnet sln add {{ProjectName}}.WebApi/{{ProjectName}}.WebApi.csproj

# Navigate vào project
cd {{ProjectName}}.WebApi

# Add project references
dotnet add reference ../{{ProjectName}}.Application/{{ProjectName}}.Application.csproj
dotnet add reference ../{{ProjectName}}.Infrastructure/{{ProjectName}}.Infrastructure.csproj

# Add packages
dotnet add package Swashbuckle.AspNetCore --version 7.2.0
dotnet add package DotNetEnv --version 3.1.1

# Tạo cấu trúc thư mục
mkdir Controllers
mkdir Middleware
mkdir Filters
mkdir Attributes
mkdir Configuration

cd ..
```

### 1.6. Verify Solution Structure

```bash
dotnet build
```

## Bước 2: Implement Domain Layer

### 2.1. Tạo Base Classes

```csharp
// {{ProjectName}}.Domain/Common/Entity.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace {{ProjectName}}.Domain.Common;

public abstract class Entity
{
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    private readonly List<BaseEvent> _domainEvents = new();

    [NotMapped]
    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

```csharp
// {{ProjectName}}.Domain/Common/BaseEvent.cs
using MediatR;

namespace {{ProjectName}}.Domain.Common;

public abstract class BaseEvent : INotification
{
    public DateTime OccurredOn { get; protected set; } = DateTime.UtcNow;
}
```

```csharp
// {{ProjectName}}.Domain/Common/IAuditableEntity.cs
namespace {{ProjectName}}.Domain.Common;

public interface IAuditableEntity
{
    Guid CreatedBy { get; set; }
    DateTime CreatedOn { get; }
    Guid LastModifiedBy { get; set; }
    DateTime? LastModifiedOn { get; set; }
}

public interface ISoftDelete
{
    DateTime? DeletedOn { get; set; }
    Guid? DeletedBy { get; set; }
}

public abstract class AuditableEntity : Entity, IAuditableEntity, ISoftDelete
{
    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [Column("created_on")]
    public DateTime CreatedOn { get; private set; } = DateTime.UtcNow;

    [Column("last_modified_by")]
    public Guid LastModifiedBy { get; set; }

    [Column("last_modified_on")]
    public DateTime? LastModifiedOn { get; set; }

    [Column("deleted_on")]
    public DateTime? DeletedOn { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }
}
```

```csharp
// {{ProjectName}}.Domain/Common/IAggregateRoot.cs
namespace {{ProjectName}}.Domain.Common;

public interface IAggregateRoot
{
}
```

### 2.2. Tạo Repository Interfaces

```csharp
// {{ProjectName}}.Domain/Common/IRepository.cs
using System.Linq.Expressions;

namespace {{ProjectName}}.Domain.Common;

public interface IReadRepository<T> where T : Entity, IAggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<List<T>> ListAsync(CancellationToken cancellationToken = default);
    Task<List<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}

public interface IRepository<T> : IReadRepository<T> where T : Entity, IAggregateRoot
{
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### 2.3. Tạo Domain Entities

```csharp
// {{ProjectName}}.Domain/Entities/Project.cs
using System.ComponentModel.DataAnnotations.Schema;
using {{ProjectName}}.Domain.Common;

namespace {{ProjectName}}.Domain.Entities;

[Table("projects")]
public class Project : AuditableEntity, IAggregateRoot
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("owner_id")]
    public Guid OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    [Column("is_enabled")]
    public bool IsEnabled { get; set; } = true;

    public Project() { }

    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public void ToggleEnabled()
    {
        IsEnabled = !IsEnabled;
    }
}
```

```csharp
// {{ProjectName}}.Domain/Entities/User.cs
using System.ComponentModel.DataAnnotations.Schema;
using {{ProjectName}}.Domain.Common;

namespace {{ProjectName}}.Domain.Entities;

[Table("users")]
public class User : AuditableEntity, IAggregateRoot
{
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash")]
    public string? PasswordHash { get; set; }

    [Column("salt")]
    public string? Salt { get; set; }

    [Column("is_email_verified")]
    public bool IsEmailVerified { get; set; }

    public User() { }
}
```

## Bước 3: Implement Application Layer

### 3.1. Tạo IApplicationDbContext

```csharp
// {{ProjectName}}.Application/Common/IApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using {{ProjectName}}.Domain.Entities;

namespace {{ProjectName}}.Application.Common;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Project> Projects { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### 3.2. Tạo Validation Behavior

```csharp
// {{ProjectName}}.Application/Common/Behaviors/ValidationBehavior.cs
using FluentValidation;
using MediatR;

namespace {{ProjectName}}.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Any())
        {
            throw new Exceptions.ValidationException(
                failures.Select(f => f.ErrorMessage));
        }

        return await next();
    }
}
```

### 3.3. Tạo Custom Exceptions

```csharp
// {{ProjectName}}.Application/Common/Exceptions/ValidationException.cs
namespace {{ProjectName}}.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public IEnumerable<string> Errors { get; }

    public ValidationException(IEnumerable<string> errors)
        : base("One or more validation failures have occurred.")
    {
        Errors = errors;
    }
}
```

```csharp
// {{ProjectName}}.Application/Common/Exceptions/NotFoundException.cs
namespace {{ProjectName}}.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.")
    {
    }
}
```

### 3.4. Tạo Command/Query Example

```csharp
// {{ProjectName}}.Application/Projects/Commands/CreateProjectCommand.cs
using MediatR;
using {{ProjectName}}.Domain.Common;
using {{ProjectName}}.Domain.Entities;

namespace {{ProjectName}}.Application.Projects.Commands;

public record CreateProjectCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Guid>
{
    private readonly IRepository<Project> _projectRepository;

    public CreateProjectCommandHandler(IRepository<Project> projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            OwnerId = Guid.Empty // TODO: Get from current user
        };

        await _projectRepository.AddAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        return project.Id;
    }
}
```

### 3.5. Tạo Startup.cs

```csharp
// {{ProjectName}}.Application/Startup.cs
using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using {{ProjectName}}.Application.Common.Behaviors;

namespace {{ProjectName}}.Application;

public static class Startup
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
```

## Bước 4: Implement Infrastructure Layer

### 4.1. Tạo DbContext

```csharp
// {{ProjectName}}.Infrastructure/Persistence/Context/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using {{ProjectName}}.Application.Common;
using {{ProjectName}}.Domain.Entities;
using {{ProjectName}}.Domain.Common;

namespace {{ProjectName}}.Infrastructure.Persistence.Context;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure entities
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ISoftDelete.DeletedOn));
                var condition = Expression.Equal(property, Expression.Constant(null, typeof(DateTime?)));
                var lambda = Expression.Lambda(condition, parameter);
                
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update audit fields
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = Guid.Empty; // TODO: Get from current user
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedBy = Guid.Empty; // TODO: Get from current user
                    entry.Entity.LastModifiedOn = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
```

### 4.2. Implement Repository

*(Code tương tự như trong Infrastructure Layer Guide)*

### 4.3. Tạo Startup.cs

```csharp
// {{ProjectName}}.Infrastructure/Startup.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using {{ProjectName}}.Application.Common;
using {{ProjectName}}.Domain.Common;
using {{ProjectName}}.Infrastructure.Persistence.Context;
using {{ProjectName}}.Infrastructure.Persistence.Repositories;

namespace {{ProjectName}}.Infrastructure;

public static class Startup
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped(typeof(IRepository<>), typeof(ApplicationDbRepository<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(ApplicationDbRepository<>));

        return services;
    }
}
```

## Bước 5: Implement WebAPI Layer

### 5.1. Tạo Program.cs

*(Code như trong WebAPI Layer Guide)*

### 5.2. Tạo BaseApiController

```csharp
// {{ProjectName}}.WebApi/Controllers/BaseApiController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace {{ProjectName}}.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}
```

### 5.3. Tạo appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database={{ProjectName}};Username=postgres;Password=yourpassword"
  },
  "JwtSettings": {
    "Key": "your-super-secret-key-at-least-32-characters-long",
    "Issuer": "{{ProjectName}}API",
    "Audience": "{{ProjectName}}Client",
    "ExpiryMinutes": 60
  }
}
```

## Bước 6: Create và Apply Migrations

```bash
# Install EF Core tools (nếu chưa có)
dotnet tool install --global dotnet-ef

# Navigate to Infrastructure project
cd {{ProjectName}}.Infrastructure

# Create migration
dotnet ef migrations add InitialCreate --startup-project ../{{ProjectName}}.WebApi

# Apply migration
dotnet ef database update --startup-project ../{{ProjectName}}.WebApi
```

## Bước 7: Run và Test

```bash
# Build solution
dotnet build

# Run WebAPI
cd {{ProjectName}}.WebApi
dotnet run

# Hoặc với watch mode
dotnet watch run
```

Truy cập Swagger: `https://localhost:7001/swagger`

## Bước 8: Git Setup

```bash
# Initialize git
git init

# Create .gitignore
cat > .gitignore << EOF
bin/
obj/
.vs/
*.user
*.suo
appsettings.Development.json
.env
EOF

# Initial commit
git add .
git commit -m "Initial project setup with Clean Architecture and CQRS"
```

## Next Steps

- [ ] Implement authentication (JWT)
- [ ] Add authorization
- [ ] Create more entities
- [ ] Add more commands/queries
- [ ] Implement file upload
- [ ] Add logging
- [ ] Add integration tests
- [ ] Add unit tests
- [ ] Setup CI/CD
- [ ] Deploy to production

## Troubleshooting

### Lỗi Migration

```bash
# Remove last migration
dotnet ef migrations remove --startup-project ../{{ProjectName}}.WebApi

# Clear database
dotnet ef database drop --startup-project ../{{ProjectName}}.WebApi
```

### Lỗi Dependencies

```bash
# Restore packages
dotnet restore

# Clean và rebuild
dotnet clean
dotnet build
```

### Lỗi Connection String

- Kiểm tra PostgreSQL đang chạy
- Kiểm tra username/password
- Kiểm tra database exists

---

**Congratulations!** Bạn đã tạo xong một dự án Clean Architecture với CQRS Pattern hoàn chỉnh.
