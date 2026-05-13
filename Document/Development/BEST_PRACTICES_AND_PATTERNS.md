# Best Practices và Design Patterns

## Tổng Quan

Document này tổng hợp các best practices và design patterns được áp dụng trong dự án Clean Architecture với CQRS.

## Clean Architecture Principles

### 1. Dependency Rule

**Nguyên tắc vàng**: Dependencies chỉ được point vào trong (inward), không được point ra ngoài (outward).

```
WebAPI ──→ Application ──→ Domain
  ↓            ↑
Infrastructure ─┘
```

**DO ✅**:
- Domain không phụ thuộc gì
- Application chỉ phụ thuộc Domain
- Infrastructure phụ thuộc Domain và Application
- WebAPI phụ thuộc tất cả

**DON'T ❌**:
- Domain references Application/Infrastructure
- Application references Infrastructure
- Infrastructure references WebAPI

### 2. Separation of Concerns

Mỗi layer có trách nhiệm riêng biệt:

**Domain Layer**:
- ✅ Business logic và business rules
- ✅ Domain models và entities
- ✅ Value objects
- ❌ KHÔNG có database logic
- ❌ KHÔNG có external service calls

**Application Layer**:
- ✅ Use cases và application logic
- ✅ Commands và Queries (CQRS)
- ✅ DTOs và mapping
- ❌ KHÔNG có infrastructure concerns
- ❌ KHÔNG có UI logic

**Infrastructure Layer**:
- ✅ Database implementation
- ✅ External services
- ✅ File system access
- ❌ KHÔNG có business logic

**Presentation Layer (WebAPI)**:
- ✅ HTTP concerns
- ✅ Routing
- ✅ Authentication/Authorization
- ❌ KHÔNG có business logic

### 3. Testability

Mỗi layer phải có khả năng test độc lập:

```csharp
// Domain - Pure logic, easy to test
[Fact]
public void Project_ToggleEnabled_ShouldInvertStatus()
{
    var project = new Project { IsEnabled = true };
    project.ToggleEnabled();
    Assert.False(project.IsEnabled);
}

// Application - Mock dependencies
[Fact]
public async Task CreateProject_ValidCommand_ReturnsProjectId()
{
    var mockRepo = new Mock<IRepository<Project>>();
    var handler = new CreateProjectCommandHandler(mockRepo.Object);
    
    var result = await handler.Handle(command, CancellationToken.None);
    
    Assert.NotEqual(Guid.Empty, result);
}
```

## CQRS Pattern Best Practices

### 1. Command Design

**Commands** modify state:

```csharp
// ✅ GOOD: Clear intent, immutable
public record CreateProjectCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

// ❌ BAD: Mutable, unclear
public class CreateProjectRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
}
```

**Best Practices**:
- ✅ Use `record` cho immutability
- ✅ Suffix với `Command`
- ✅ Return type: specific response hoặc `Unit`
- ✅ One command = One responsibility
- ✅ Validate trong FluentValidation
- ❌ KHÔNG return entities

### 2. Query Design

**Queries** chỉ đọc data:

```csharp
// ✅ GOOD: Read-only, returns DTO
public class GetProjectByIdQuery : IRequest<ProjectDto>
{
    public Guid Id { get; set; }
}

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDto>
{
    public async Task<ProjectDto> Handle(...)
    {
        return await _dbContext.Projects
            .AsNoTracking()  // Important for read-only
            .Where(p => p.Id == request.Id)
            .Select(p => new ProjectDto { ... })
            .FirstOrDefaultAsync();
    }
}
```

**Best Practices**:
- ✅ Suffix với `Query`
- ✅ Always return DTOs
- ✅ Use `AsNoTracking()` cho queries
- ✅ Project to DTOs với `Select()`
- ✅ Có thể query trực tiếp DbContext (bypass repository)
- ❌ KHÔNG modify data trong query handlers

### 3. Handler Design

```csharp
// ✅ GOOD: Single responsibility, clean
public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Guid>
{
    private readonly IRepository<Project> _projectRepository;
    private readonly ICurrentUser _currentUser;

    public CreateProjectCommandHandler(
        IRepository<Project> projectRepository,
        ICurrentUser currentUser)
    {
        _projectRepository = projectRepository;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateProjectCommand request, 
        CancellationToken cancellationToken)
    {
        // Clear, concise logic
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            OwnerId = _currentUser.GetUserId()
        };

        await _projectRepository.AddAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        return project.Id;
    }
}
```

## Repository Pattern

### 1. Generic Repository

```csharp
// ✅ GOOD: Interface in Domain, Implementation in Infrastructure
// Domain Layer
public interface IRepository<T> where T : Entity, IAggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

// Infrastructure Layer
public class ApplicationDbRepository<T> : IRepository<T>
    where T : Entity, IAggregateRoot
{
    // Implementation...
}
```

**Best Practices**:
- ✅ Interface trong Domain
- ✅ Implementation trong Infrastructure
- ✅ Generic cho common operations
- ✅ Constraint: `where T : Entity, IAggregateRoot`
- ✅ Async methods với CancellationToken
- ❌ KHÔNG expose IQueryable

### 2. Specific Repository (Optional)

Khi cần complex queries:

```csharp
// Application Layer
public interface IProjectRepository : IRepository<Project>
{
    Task<List<Project>> GetUserProjectsWithRolesAsync(
        Guid userId, 
        CancellationToken cancellationToken);
}

// Infrastructure Layer
public class ProjectRepository : ApplicationDbRepository<Project>, IProjectRepository
{
    public async Task<List<Project>> GetUserProjectsWithRolesAsync(
        Guid userId, 
        CancellationToken cancellationToken)
    {
        return await _dbSet
            .Include(p => p.UserProjects)
            .ThenInclude(up => up.ProjectRole)
            .Where(p => p.UserProjects.Any(up => up.UserId == userId))
            .ToListAsync(cancellationToken);
    }
}
```

## Domain-Driven Design (DDD) Patterns

### 1. Aggregate Roots

```csharp
// ✅ GOOD: Clear aggregate boundary
[Table("projects")]
public class Project : AuditableEntity, IAggregateRoot
{
    public string Name { get; set; }
    
    // Navigation to child entities
    public List<Issue> Issues { get; set; } = new();
    
    // Business method that maintains invariants
    public Issue AddIssue(string title, string description)
    {
        var issue = new Issue 
        { 
            Title = title, 
            Description = description,
            ProjectId = this.Id  // Encapsulation
        };
        
        Issues.Add(issue);
        
        // Raise domain event
        AddDomainEvent(new IssueCreatedEvent(issue.Id, this.Id));
        
        return issue;
    }
}
```

**Best Practices**:
- ✅ Chỉ Aggregate Roots có repositories
- ✅ Access child entities qua Aggregate Root
- ✅ Maintain invariants trong aggregate
- ✅ Raise domain events cho side effects

### 2. Value Objects

```csharp
// ✅ GOOD: Immutable, equality based on value
public class Address : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string Country { get; private set; }

    private Address() { } // For EF

    public Address(string street, string city, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street cannot be empty");
            
        Street = street;
        City = city;
        Country = country;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return Country;
    }
}
```

**Best Practices**:
- ✅ Immutable (private setters)
- ✅ Equality based on values
- ✅ Validation trong constructor
- ✅ Rich behavior

### 3. Domain Events

```csharp
// Event definition
public class ProjectCreatedEvent : BaseEvent
{
    public Guid ProjectId { get; }
    public Guid OwnerId { get; }

    public ProjectCreatedEvent(Guid projectId, Guid ownerId)
    {
        ProjectId = projectId;
        OwnerId = ownerId;
    }
}

// Raise event
public class Project : AuditableEntity, IAggregateRoot
{
    public static Project Create(string name, Guid ownerId)
    {
        var project = new Project 
        { 
            Name = name, 
            OwnerId = ownerId 
        };
        
        project.AddDomainEvent(new ProjectCreatedEvent(project.Id, ownerId));
        
        return project;
    }
}

// Handle event
public class ProjectCreatedEventHandler : INotificationHandler<ProjectCreatedEvent>
{
    private readonly IEmailService _emailService;

    public async Task Handle(ProjectCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Send welcome email, create audit log, etc.
        await _emailService.SendProjectCreatedNotificationAsync(notification.OwnerId);
    }
}
```

## Validation Patterns

### 1. FluentValidation

```csharp
// ✅ GOOD: Comprehensive validation
public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters")
            .Matches(@"^[a-zA-Z0-9\s-]+$").WithMessage("Name contains invalid characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(1000);
    }
}
```

### 2. Async Validation với Database

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

## Error Handling

### 1. Custom Exceptions

```csharp
// Define exceptions
public class ValidationException : Exception
{
    public IEnumerable<string> Errors { get; }

    public ValidationException(IEnumerable<string> errors)
        : base("Validation failed")
    {
        Errors = errors;
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) not found")
    {
    }
}
```

### 2. Global Exception Handler

```csharp
// Middleware
public class ExceptionHandlingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            ValidationException validationEx => new ErrorResponse
            {
                StatusCode = 400,
                Message = "Validation failed",
                Errors = validationEx.Errors
            },
            NotFoundException notFoundEx => new ErrorResponse
            {
                StatusCode = 404,
                Message = notFoundEx.Message
            },
            UnauthorizedAccessException => new ErrorResponse
            {
                StatusCode = 401,
                Message = "Unauthorized"
            },
            _ => new ErrorResponse
            {
                StatusCode = 500,
                Message = "Internal server error"
            }
        };

        context.Response.StatusCode = response.StatusCode;
        await context.Response.WriteAsJsonAsync(response);
    }
}
```

## Entity Framework Core Best Practices

### 1. DbContext Configuration

```csharp
// ✅ GOOD: Registered as Scoped
services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.EnableSensitiveDataLogging(isDevelopment);
    options.EnableDetailedErrors(isDevelopment);
});

// Register interface
services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());
```

### 2. Query Optimization

```csharp
// ✅ GOOD: AsNoTracking for read-only queries
var projects = await _dbContext.Projects
    .AsNoTracking()
    .Where(p => p.IsEnabled)
    .Select(p => new ProjectDto 
    { 
        Id = p.Id,
        Name = p.Name 
    })
    .ToListAsync();

// ✅ GOOD: Include for eager loading
var project = await _dbContext.Projects
    .Include(p => p.Owner)
    .Include(p => p.UserProjects)
        .ThenInclude(up => up.User)
    .FirstOrDefaultAsync(p => p.Id == id);

// ❌ BAD: N+1 queries
foreach (var project in projects)
{
    var owner = await _dbContext.Users.FindAsync(project.OwnerId); // N+1!
}
```

### 3. Soft Delete

```csharp
// Global query filter
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Apply to all ISoftDelete entities
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

// Override Delete to Soft Delete
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
    {
        if (entry.State == EntityState.Deleted)
        {
            entry.State = EntityState.Modified;
            entry.Entity.DeletedOn = DateTime.UtcNow;
            entry.Entity.DeletedBy = _currentUser.GetUserId();
        }
    }
    
    return base.SaveChangesAsync(cancellationToken);
}
```

## Security Best Practices

### 1. Authentication

```csharp
// JWT Configuration
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero  // No tolerance
        };
    });
```

### 2. Authorization

```csharp
// Custom authorization attribute
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class MustBeAuthenticatedAttribute : AuthorizeAttribute
{
}

// Usage in controller
[HttpPost]
[MustBeAuthenticated]
public async Task<IActionResult> CreateProject(CreateProjectCommand command)
{
    var result = await Mediator.Send(command);
    return Ok(result);
}
```

### 3. Password Hashing

```csharp
// ✅ GOOD: Use BCrypt or similar
public class PasswordHasher
{
    public (string hash, string salt) HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[16];
        rng.GetBytes(salt);
        
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password, 
            salt, 
            iterations: 100000, 
            HashAlgorithmName.SHA256);
            
        var hash = pbkdf2.GetBytes(32);
        
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password, 
            saltBytes, 
            iterations: 100000, 
            HashAlgorithmName.SHA256);
            
        var computedHash = pbkdf2.GetBytes(32);
        var storedHash = Convert.FromBase64String(hash);
        
        return computedHash.SequenceEqual(storedHash);
    }
}
```

## Performance Best Practices

### 1. Async/Await

```csharp
// ✅ GOOD
public async Task<ProjectDto> GetProjectAsync(Guid id)
{
    return await _dbContext.Projects
        .AsNoTracking()
        .Where(p => p.Id == id)
        .Select(p => new ProjectDto { ... })
        .FirstOrDefaultAsync();
}

// ❌ BAD: Blocking call
public ProjectDto GetProject(Guid id)
{
    return _dbContext.Projects
        .Where(p => p.Id == id)
        .Select(p => new ProjectDto { ... })
        .FirstOrDefault();  // Synchronous!
}
```

### 2. Projection cho DTOs

```csharp
// ✅ GOOD: Project trong database
var projects = await _dbContext.Projects
    .Select(p => new ProjectDto
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description
    })
    .ToListAsync();

// ❌ BAD: Load full entities then project
var projects = await _dbContext.Projects.ToListAsync();
var dtos = projects.Select(p => new ProjectDto { ... }).ToList();
```

### 3. Pagination

```csharp
public class GetProjectsQuery : IRequest<PaginatedList<ProjectDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PaginatedList<T>
{
    public List<T> Items { get; }
    public int PageNumber { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }

    public PaginatedList(List<T> items, int count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
    }

    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source, 
        int pageNumber, 
        int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
        return new PaginatedList<T>(items, count, pageNumber, pageSize);
    }
}
```

## Testing Best Practices

### 1. Unit Tests

```csharp
public class ProjectTests
{
    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        // Arrange
        var project = new Project { Name = "Old", Description = "Old Desc" };

        // Act
        project.Update("New Name", "New Description");

        // Assert
        Assert.Equal("New Name", project.Name);
        Assert.Equal("New Description", project.Description);
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData("Name", "")]
    public void Update_WithInvalidData_ThrowsException(string name, string description)
    {
        var project = new Project();
        
        Assert.Throws<ArgumentException>(() => 
            project.Update(name, description));
    }
}
```

### 2. Integration Tests

```csharp
public class ProjectControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProjectControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProject_ValidRequest_ReturnsOk()
    {
        // Arrange
        var command = new CreateProjectCommand
        {
            Name = "Test Project",
            Description = "Test Description"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/project", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(result);
        Assert.Equal("Test Project", result.Name);
    }
}
```

## Summary Checklists

### ✅ Clean Architecture Checklist
- [ ] Dependencies point inward only
- [ ] Each layer has single responsibility
- [ ] Interfaces in inner layers, implementations in outer
- [ ] Domain layer has no dependencies
- [ ] Application layer only depends on Domain
- [ ] Infrastructure implements Application interfaces

### ✅ CQRS Checklist
- [ ] Commands modify state, Queries read only
- [ ] Commands return Unit or specific responses
- [ ] Queries return DTOs
- [ ] Separate handlers for each command/query
- [ ] Validation với FluentValidation
- [ ] AsNoTracking() cho read queries

### ✅ DDD Checklist
- [ ] Rich domain models với business logic
- [ ] Aggregate roots properly defined
- [ ] Value objects for complex values
- [ ] Domain events for side effects
- [ ] Repository chỉ cho aggregate roots

### ✅ Security Checklist
- [ ] JWT authentication configured
- [ ] Passwords properly hashed
- [ ] HTTPS enforced
- [ ] CORS configured correctly
- [ ] Sensitive data in environment variables
- [ ] Input validation everywhere

### ✅ Performance Checklist
- [ ] Async/await throughout
- [ ] AsNoTracking() for read queries
- [ ] Proper eager loading (Include)
- [ ] Pagination for large datasets
- [ ] Projection to DTOs in database
- [ ] Query optimization

---

**Remember**: These are guidelines, not rules. Adapt based on your specific requirements!
