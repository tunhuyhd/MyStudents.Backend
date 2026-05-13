# Hướng Dẫn Application Layer với CQRS Pattern

## Mục Đích

**Application Layer** chứa business logic và orchestrates use cases của ứng dụng:

- Triển khai CQRS pattern (Commands và Queries)
- Validation với FluentValidation
- Pipeline behaviors (logging, validation, transaction)
- DTOs cho data transfer
- Interfaces cho external services

**Nguyên tắc**: Application Layer CHỈ phụ thuộc vào Domain Layer.

## Cấu Trúc Thư Mục

```
{{ProjectName}}.Application/
├── {{ProjectName}}.Application.csproj
├── Startup.cs                          # Service registration
├── Common/                             # Shared components
│   ├── IApplicationDbContext.cs        # DbContext interface
│   ├── Authorization/                  # Authorization services
│   ├── Behaviors/                      # MediatR behaviors
│   │   └── ValidationBehavior.cs
│   ├── Dto/                           # Data Transfer Objects
│   ├── Exceptions/                    # Custom exceptions
│   │   └── ValidationException.cs
│   ├── Extensions/                    # Extension methods
│   ├── Interfaces/                    # Service interfaces
│   ├── Response/                      # Response models
│   └── Services/                      # Service implementations
├── Projects/                          # Feature: Projects
│   ├── Commands/                      # Write operations
│   │   ├── CreateProjectCommand.cs
│   │   ├── UpdateProjectCommand.cs
│   │   └── DeleteProjectCommand.cs
│   └── Queries/                       # Read operations
│       ├── GetProjectByIdQuery.cs
│       └── GetMyProjectsQuery.cs
├── Users/                             # Feature: Users
│   ├── Commands/
│   └── Queries/
└── Identity/
    └── ITokenService.cs
```

## Dependencies

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Domain Layer -->
    <ProjectReference Include="..\{{ProjectName}}.Domain\{{ProjectName}}.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- CQRS với MediatR -->
    <PackageReference Include="MediatR" Version="14.0.0" />
    
    <!-- Validation -->
    <PackageReference Include="FluentValidation" Version="12.1.1" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.1.1" />
    
    <!-- Entity Framework (chỉ interfaces, không có implementation) -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.2" />
  </ItemGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
</Project>
```

## Service Registration (Startup.cs)

```csharp
// Startup.cs
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

        // Register MediatR với tất cả handlers
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(assembly));

        // Register tất cả FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register pipeline behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>));

        // Có thể thêm behaviors khác:
        // services.AddTransient(typeof(IPipelineBehavior<,>),
        //     typeof(LoggingBehavior<,>));
        // services.AddTransient(typeof(IPipelineBehavior<,>),
        //     typeof(TransactionBehavior<,>));

        return services;
    }
}
```

## CQRS Pattern Implementation

### 1. Commands (Write Operations)

Commands thay đổi state của application.

#### Ví Dụ: Create Project Command

```csharp
// Projects/Commands/CreateProjectCommand.cs
using {{ProjectName}}.Application.Common.Response;
using MediatR;

namespace {{ProjectName}}.Application.Projects.Commands;

// Command Request
public record CreateProjectCommand : IRequest<CreateProjectResponse>
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

// Command Handler
public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, CreateProjectResponse>
{
    private readonly IRepository<Project> _projectRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IRepository<UserProject> _userProjectRepository;
    private readonly IApplicationDbContext _dbContext;

    public CreateProjectCommandHandler(
        IRepository<Project> projectRepository,
        ICurrentUser currentUser,
        IRepository<UserProject> userProjectRepository,
        IApplicationDbContext dbContext)
    {
        _projectRepository = projectRepository;
        _currentUser = currentUser;
        _userProjectRepository = userProjectRepository;
        _dbContext = dbContext;
    }

    public async Task<CreateProjectResponse> Handle(
        CreateProjectCommand request, 
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUser.GetUserId();

        // Business logic
        var projectRoleId = await _dbContext.ProjectRoles
            .Where(pr => pr.Code == ProjectRoleCode.ProjectAdmin)
            .Select(pr => pr.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // Create entity
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            OwnerId = currentUserId
        };

        // Save to repository
        await _projectRepository.AddAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        // Create user-project relationship
        var userProject = new UserProject
        {
            UserId = currentUserId,
            ProjectId = project.Id,
            ProjectRoleId = projectRoleId,
        };

        await _userProjectRepository.AddAsync(userProject, cancellationToken);
        await _userProjectRepository.SaveChangesAsync(cancellationToken);

        // Return response
        return new CreateProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            OwnerId = project.OwnerId.ToString(),
            OwnerName = _currentUser.GetUsername(),
        };
    }
}
```

**Best Practices cho Commands**:

- ✅ Use `record` cho immutable commands
- ✅ Suffix với `Command`
- ✅ Handler suffix với `CommandHandler`
- ✅ Return specific response type hoặc `Unit` (void)
- ✅ Commands nên thay đổi 1 aggregate hoặc có transaction
- ❌ KHÔNG return entities trực tiếp, use DTOs

#### Ví Dụ: Update Command

```csharp
// Projects/Commands/UpdateProjectCommand.cs
using MediatR;

namespace {{ProjectName}}.Application.Projects.Commands;

public record UpdateProjectCommand : IRequest<UpdateProjectResponse>
{
    public Guid Id { get; set; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, UpdateProjectResponse>
{
    private readonly IRepository<Project> _projectRepository;

    public UpdateProjectCommandHandler(IRepository<Project> projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<UpdateProjectResponse> Handle(
        UpdateProjectCommand request, 
        CancellationToken cancellationToken)
    {
        // Get entity
        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (project == null)
            throw new NotFoundException(nameof(Project), request.Id);

        // Use domain method
        project.Update(request.Name, request.Description);

        // Save changes
        await _projectRepository.UpdateAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        return new UpdateProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description
        };
    }
}
```

#### Ví Dụ: Delete Command

```csharp
// Projects/Commands/DeleteProjectCommand.cs
using MediatR;

namespace {{ProjectName}}.Application.Projects.Commands;

public record DeleteProjectCommand : IRequest<Unit>
{
    public Guid Id { get; init; }
}

public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, Unit>
{
    private readonly IRepository<Project> _projectRepository;

    public DeleteProjectCommandHandler(IRepository<Project> projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Unit> Handle(
        DeleteProjectCommand request, 
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (project == null)
            throw new NotFoundException(nameof(Project), request.Id);

        await _projectRepository.DeleteAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
```

### 2. Queries (Read Operations)

Queries chỉ đọc dữ liệu, không thay đổi state.

#### Ví Dụ: Get By Id Query

```csharp
// Projects/Queries/GetProjectByIdQuery.cs
using {{ProjectName}}.Application.Common;
using {{ProjectName}}.Application.Common.Dto.Projects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace {{ProjectName}}.Application.Projects.Queries;

// Query Request
public class GetProjectByIdQuery : IRequest<ProjectDetailDto>
{
    public Guid Id { get; set; }
}

// Query Handler
public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDetailDto>
{
    private readonly IApplicationDbContext _dbContext;

    public GetProjectByIdQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProjectDetailDto> Handle(
        GetProjectByIdQuery request, 
        CancellationToken cancellationToken)
    {
        // Query database directly (no repository needed for queries)
        var project = await _dbContext.Projects
            .AsNoTracking()  // For read-only queries
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (project == null)
            throw new NotFoundException(nameof(Project), request.Id);

        // Map to DTO
        return new ProjectDetailDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            IsEnabled = project.IsEnabled,
            CreatedOn = project.CreatedOn
        };
    }
}
```

**Best Practices cho Queries**:

- ✅ Suffix với `Query`
- ✅ Handler suffix với `QueryHandler`
- ✅ Return DTOs, KHÔNG return entities
- ✅ Use `AsNoTracking()` cho read-only queries
- ✅ Có thể query trực tiếp từ DbContext (bypass repository)
- ✅ Projection với `Select()` để optimize performance
- ❌ KHÔNG modify entities trong queries

#### Ví Dụ: List Query với Filtering

```csharp
// Projects/Queries/GetMyProjectsQuery.cs
using {{ProjectName}}.Application.Common;
using {{ProjectName}}.Application.Common.Dto.Projects;
using {{ProjectName}}.Application.Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace {{ProjectName}}.Application.Projects.Queries;

public class GetMyProjectsQuery : IRequest<List<ProjectDto>>
{
    public bool? IsEnabled { get; set; }
    public string? SearchTerm { get; set; }
}

public class GetMyProjectsQueryHandler : IRequestHandler<GetMyProjectsQuery, List<ProjectDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyProjectsQueryHandler(
        IApplicationDbContext dbContext, 
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<List<ProjectDto>> Handle(
        GetMyProjectsQuery request, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        // Build query
        var query = _dbContext.Projects
            .AsNoTracking()
            .Where(p => p.UserProjects.Any(up => up.UserId == userId));

        // Apply filters
        if (request.IsEnabled.HasValue)
        {
            query = query.Where(p => p.IsEnabled == request.IsEnabled.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(p => 
                p.Name.Contains(request.SearchTerm) || 
                p.Description.Contains(request.SearchTerm));
        }

        // Execute and project to DTO
        return await query
            .Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                IsEnabled = p.IsEnabled,
                CreatedOn = p.CreatedOn
            })
            .ToListAsync(cancellationToken);
    }
}
```

## Validation với FluentValidation

### 1. Create Validator

```csharp
// Projects/Commands/CreateProjectCommandValidator.cs
using FluentValidation;

namespace {{ProjectName}}.Application.Projects.Commands;

public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");
    }
}
```

### 2. Complex Validation với Database Check

```csharp
public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateUserCommandValidator(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(BeUniqueEmail)
                .WithMessage("Email already exists");
    }

    private async Task<bool> BeUniqueEmail(
        UpdateUserCommand command, 
        string email, 
        CancellationToken cancellationToken)
    {
        return !await _dbContext.Users
            .AnyAsync(u => u.Email == email && u.Id != command.Id, cancellationToken);
    }
}
```

### 3. ValidationBehavior

```csharp
// Common/Behaviors/ValidationBehavior.cs
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

## DTOs (Data Transfer Objects)

### Response DTOs

```csharp
// Common/Response/CreateProjectResponse.cs
namespace {{ProjectName}}.Application.Common.Response;

public class CreateProjectResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
}
```

### Query DTOs

```csharp
// Common/Dto/Projects/ProjectDto.cs
namespace {{ProjectName}}.Application.Common.Dto.Projects;

public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class ProjectDetailDto : ProjectDto
{
    public string OwnerName { get; set; } = string.Empty;
    public List<ProjectMemberDto> Members { get; set; } = new();
}
```

## Exceptions

### Custom Exceptions

```csharp
// Common/Exceptions/ValidationException.cs
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
// Common/Exceptions/NotFoundException.cs
namespace {{ProjectName}}.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.")
    {
    }
}
```

## Interfaces

### IApplicationDbContext

```csharp
// Common/IApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using {{ProjectName}}.Domain.Entities;

namespace {{ProjectName}}.Application.Common;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectRole> ProjectRoles { get; }
    DbSet<UserProject> UserProjects { get; }
    // ... other DbSets

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

**Lưu ý**: Interface này sẽ được implement ở Infrastructure Layer.

### ICurrentUser Service

```csharp
// Common/Authorization/ICurrentUser.cs
namespace {{ProjectName}}.Application.Common.Authorization;

public interface ICurrentUser
{
    Guid GetUserId();
    string GetUsername();
    string GetUserEmail();
    bool IsAuthenticated();
}
```

## Additional Behaviors

### Logging Behavior

```csharp
// Common/Behaviors/LoggingBehavior.cs
using MediatR;
using Microsoft.Extensions.Logging;

namespace {{ProjectName}}.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUser _currentUser;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUser currentUser)
    {
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUser.IsAuthenticated() ? _currentUser.GetUserId() : Guid.Empty;

        _logger.LogInformation("Handling {RequestName} by User {UserId}", 
            requestName, userId);

        var response = await next();

        _logger.LogInformation("Handled {RequestName}", requestName);

        return response;
    }
}
```

## Best Practices

### 1. Command/Query Design

- ✅ Commands modify state, Queries read only
- ✅ Use `record` cho immutable commands/queries
- ✅ One handler per command/query
- ✅ Keep handlers focused (Single Responsibility)
- ❌ KHÔNG mix command và query logic

### 2. Validation

- ✅ Use FluentValidation cho input validation
- ✅ Một validator cho mỗi command
- ✅ Async validation cho database checks
- ❌ KHÔNG validation ở controller level

### 3. DTOs

- ✅ Always return DTOs, never entities
- ✅ Specific DTOs cho specific use cases
- ✅ Simple POCOs, no logic
- ❌ KHÔNG expose domain entities qua API

### 4. Dependencies

- ✅ Inject via constructor
- ✅ Depend on interfaces, not implementations
- ✅ Keep dependencies minimal
- ❌ KHÔNG reference Infrastructure or WebAPI layers

### 5. Error Handling

- ✅ Throw domain exceptions
- ✅ Use custom exceptions
- ✅ Handle exceptions ở middleware level
- ❌ KHÔNG catch-all trong handlers

## Testing Application Layer

```csharp
// Tests/Application/CreateProjectCommandTests.cs
public class CreateProjectCommandTests
{
    private readonly Mock<IRepository<Project>> _projectRepository;
    private readonly Mock<ICurrentUser> _currentUser;
    private readonly CreateProjectCommandHandler _handler;

    public CreateProjectCommandTests()
    {
        _projectRepository = new Mock<IRepository<Project>>();
        _currentUser = new Mock<ICurrentUser>();
        // Setup other mocks...

        _handler = new CreateProjectCommandHandler(
            _projectRepository.Object,
            _currentUser.Object,
            // ... other dependencies
        );
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesProject()
    {
        // Arrange
        var command = new CreateProjectCommand
        {
            Name = "Test Project",
            Description = "Test Description"
        };

        _currentUser.Setup(x => x.GetUserId()).Returns(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(command.Name, result.Name);
        _projectRepository.Verify(x => x.AddAsync(
            It.IsAny<Project>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

## Checklist

- [ ] Create project `{{ProjectName}}.Application`
- [ ] Install packages: MediatR, FluentValidation
- [ ] Create `Startup.cs` for service registration
- [ ] Define `IApplicationDbContext` interface
- [ ] Create Commands với handlers
- [ ] Create Queries với handlers
- [ ] Add FluentValidation validators
- [ ] Implement ValidationBehavior
- [ ] Create DTOs for responses
- [ ] Create custom exceptions
- [ ] Add service interfaces
- [ ] Write unit tests
- [ ] Review: Ensure only depends on Domain layer

---

**Next**: [Infrastructure Layer Guide](./INFRASTRUCTURE_LAYER_GUIDE.md)
