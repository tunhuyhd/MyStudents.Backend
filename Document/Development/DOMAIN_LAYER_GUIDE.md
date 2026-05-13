# Hướng Dẫn Domain Layer

## Mục Đích

**Domain Layer** là tầng trung tâm của Clean Architecture, chứa:
- Business logic thuần túy
- Domain models (Entities)
- Business rules
- Domain events
- Repository interfaces

**Nguyên tắc quan trọng**: Domain Layer **KHÔNG PHỤ THUỘC** vào bất kỳ layer nào khác.

## Cấu Trúc Thư Mục

```
{{ProjectName}}.Domain/
├── {{ProjectName}}.Domain.csproj
├── Common/                      # Base classes và interfaces
│   ├── Entity.cs               # Base entity class
│   ├── AuditableEntity.cs      # Entity với audit fields
│   ├── IAggregateRoot.cs       # Marker interface
│   ├── IAuditableEntity.cs     # Audit interface
│   ├── IRepository.cs          # Repository interface
│   ├── IReadRepository.cs      # Read-only repository
│   └── BaseEvent.cs            # Base domain event
├── Entities/                    # Domain models
│   ├── User.cs
│   ├── Project.cs
│   ├── Permission.cs
│   ├── ProjectRole.cs
│   ├── UserProject.cs
│   └── InvitationJoiningProject.cs
└── Events/                      # Domain events
    └── (Domain event classes)
```

## Dependencies

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MediatR" Version="14.0.0" />
  </ItemGroup>
</Project>
```

**Lưu ý**: Domain chỉ phụ thuộc MediatR cho Domain Events, không có dependency nào khác.

## Tạo Base Classes

### 1. Entity Base Class

```csharp
// Common/Entity.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace {{ProjectName}}.Domain.Common;

public abstract class Entity
{
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    private readonly List<BaseEvent> _domainEvents = new();

    [NotMapped]
    public IReadOnlyCollection<BaseEvent> DomainEvents => 
        _domainEvents.AsReadOnly();

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

**Giải thích**:
- `Id`: Unique identifier cho mỗi entity
- `DomainEvents`: Collection để lưu domain events
- Các methods để manage domain events

### 2. IAuditableEntity Interface

```csharp
// Common/IAuditableEntity.cs
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
```

### 3. AuditableEntity Base Class

```csharp
// Common/AuditableEntity.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace {{ProjectName}}.Domain.Common;

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

**Giải thích**:
- Kế thừa từ `Entity`
- Tự động track audit information (Created, Modified, Deleted)
- Support soft delete pattern

### 4. IAggregateRoot Interface

```csharp
// Common/IAggregateRoot.cs
namespace {{ProjectName}}.Domain.Common;

/// <summary>
/// Marker interface để định nghĩa Aggregate Root trong DDD
/// Chỉ Aggregate Roots mới có Repository
/// </summary>
public interface IAggregateRoot
{
}
```

**Giải thích**:
- Marker interface theo DDD pattern
- Chỉ entities implement `IAggregateRoot` mới có generic repository
- Aggregate Root là entry point để truy cập các entities liên quan

## Tạo Repository Interfaces

### 1. IReadRepository Interface

```csharp
// Common/IReadRepository.cs
using System.Linq.Expressions;

namespace {{ProjectName}}.Domain.Common;

public interface IReadRepository<T> where T : Entity, IAggregateRoot
{
    Task<T?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<List<T>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<List<T>> ListAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);
}
```

### 2. IRepository Interface

```csharp
// Common/IRepository.cs
namespace {{ProjectName}}.Domain.Common;

public interface IRepository<T> : IReadRepository<T> 
    where T : Entity, IAggregateRoot
{
    Task<T> AddAsync(
        T entity,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> AddRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        T entity,
        CancellationToken cancellationToken = default);

    Task UpdateRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        T entity,
        CancellationToken cancellationToken = default);

    Task DeleteRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
```

**Giải thích**:
- Generic repository pattern
- Constraint: `where T : Entity, IAggregateRoot`
- Separation: Read operations vs Write operations
- **Chỉ define interface**, implementation sẽ ở Infrastructure Layer

## Tạo Domain Entities

### Ví Dụ: Project Entity

```csharp
// Entities/Project.cs
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

    // Navigation properties
    public User Owner { get; set; } = null!;

    [Column("is_enabled")]
    public bool IsEnabled { get; set; } = true;

    public List<UserProject> UserProjects { get; set; } = [];

    // Constructor
    public Project() { }

    // Business methods
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

**Best Practices cho Entities**:

1. **Kế thừa từ AuditableEntity**: Tự động có audit fields
2. **Implement IAggregateRoot**: Nếu entity là aggregate root
3. **Table attribute**: Specify table name (snake_case cho PostgreSQL)
4. **Column attribute**: Specify column names
5. **Business methods**: Encapsulate business logic trong entity
6. **Validation**: Domain validation trong methods
7. **Navigation properties**: Cho EF Core relationships

### Ví Dụ: User Entity

```csharp
// Entities/User.cs
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

    [Column("google_id")]
    public string? GoogleId { get; set; }

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    // Navigation properties
    public List<Project> OwnedProjects { get; set; } = [];
    public List<UserProject> UserProjects { get; set; } = [];

    // Business methods
    public void UpdateProfile(string? avatarUrl)
    {
        if (!string.IsNullOrWhiteSpace(avatarUrl))
            AvatarUrl = avatarUrl;
    }

    public void VerifyEmail()
    {
        IsEmailVerified = true;
    }
}
```

## Domain Events

### 1. Base Event Class

```csharp
// Common/BaseEvent.cs
using MediatR;

namespace {{ProjectName}}.Domain.Common;

public abstract class BaseEvent : INotification
{
    public DateTime OccurredOn { get; protected set; } = DateTime.UtcNow;
}
```

### 2. Ví Dụ Domain Event

```csharp
// Events/ProjectCreatedEvent.cs
using {{ProjectName}}.Domain.Common;

namespace {{ProjectName}}.Domain.Events;

public class ProjectCreatedEvent : BaseEvent
{
    public Guid ProjectId { get; }
    public Guid OwnerId { get; }
    public string ProjectName { get; }

    public ProjectCreatedEvent(Guid projectId, Guid ownerId, string projectName)
    {
        ProjectId = projectId;
        OwnerId = ownerId;
        ProjectName = projectName;
    }
}
```

### 3. Raise Domain Event trong Entity

```csharp
public class Project : AuditableEntity, IAggregateRoot
{
    // ... properties ...

    public static Project Create(string name, string description, Guid ownerId)
    {
        var project = new Project
        {
            Name = name,
            Description = description,
            OwnerId = ownerId
        };

        // Raise domain event
        project.AddDomainEvent(new ProjectCreatedEvent(
            project.Id, 
            ownerId, 
            name
        ));

        return project;
    }
}
```

## Value Objects (Optional)

Value Objects là objects không có identity, chỉ được định nghĩa bởi attributes:

```csharp
// Common/ValueObject.cs
namespace {{ProjectName}}.Domain.Common;

public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;

        var valueObject = (ValueObject)obj;
        return GetEqualityComponents()
            .SequenceEqual(valueObject.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }
}
```

### Ví Dụ: Address Value Object

```csharp
// ValueObjects/Address.cs
namespace {{ProjectName}}.Domain.ValueObjects;

public class Address : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string Country { get; private set; }

    private Address() { } // For EF

    public Address(string street, string city, string country)
    {
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

## Enumerations

```csharp
// Entities/Enum/ProjectRoleCode.cs
namespace {{ProjectName}}.Domain.Entities.Enum;

public static class ProjectRoleCode
{
    public const string ProjectAdmin = "PROJECT_ADMIN";
    public const string ProjectMember = "PROJECT_MEMBER";
    public const string ProjectViewer = "PROJECT_VIEWER";
}
```

**Lưu ý**: Sử dụng constants thay vì enum cho flexibility và database mapping.

## Best Practices

### 1. Entity Design
- ✅ Rich domain models với business logic
- ✅ Encapsulation: Private setters, business methods
- ✅ Validation trong domain methods
- ❌ Anemic domain models (chỉ có properties)

### 2. Naming Conventions
- ✅ PascalCase cho properties
- ✅ snake_case cho database columns (PostgreSQL)
- ✅ Descriptive names

### 3. Dependencies
- ✅ NO dependency trừ MediatR
- ❌ KHÔNG reference Application, Infrastructure, WebAPI layers
- ❌ KHÔNG reference Entity Framework trong domain logic

### 4. Business Logic
- ✅ Business rules trong entity methods
- ✅ Domain validation trong entities
- ✅ Domain events cho side effects
- ❌ Business logic KHÔNG nên ở Application hoặc Infrastructure

### 5. Aggregate Roots
- ✅ Chỉ Aggregate Roots implement `IAggregateRoot`
- ✅ Chỉ Aggregate Roots có repositories
- ✅ Access child entities qua Aggregate Root

## Testing Domain Layer

```csharp
// Tests/Domain/ProjectTests.cs
public class ProjectTests
{
    [Fact]
    public void Update_ShouldChangeNameAndDescription()
    {
        // Arrange
        var project = new Project 
        { 
            Name = "Old Name", 
            Description = "Old Desc" 
        };

        // Act
        project.Update("New Name", "New Description");

        // Assert
        Assert.Equal("New Name", project.Name);
        Assert.Equal("New Description", project.Description);
    }

    [Fact]
    public void ToggleEnabled_ShouldInvertIsEnabled()
    {
        // Arrange
        var project = new Project { IsEnabled = true };

        // Act
        project.ToggleEnabled();

        // Assert
        Assert.False(project.IsEnabled);
    }
}
```

## Checklist Tạo Domain Layer

- [ ] Tạo project `{{ProjectName}}.Domain`
- [ ] Cài đặt package `MediatR`
- [ ] Tạo base classes: `Entity`, `AuditableEntity`
- [ ] Tạo interfaces: `IAggregateRoot`, `IRepository`, `IReadRepository`
- [ ] Tạo domain entities với business logic
- [ ] Implement domain events (nếu cần)
- [ ] Tạo value objects (nếu cần)
- [ ] Định nghĩa enumerations/constants
- [ ] Viết unit tests cho domain logic
- [ ] Review: Đảm bảo NO dependency ngoài MediatR

---

**Next**: [Application Layer Guide](./DOMAIN_LAYER_APPLICATION_GUIDE.md)
