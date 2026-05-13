# Tổng Quan Kiến Trúc Dự Án {{ProjectName}}

## Giới thiệu

Dự án {{ProjectName}} được xây dựng dựa trên **Clean Architecture** kết hợp với **CQRS Pattern** (Command Query Responsibility Segregation), tạo nên một kiến trúc phần mềm có tính mở rộng cao, dễ bảo trì và kiểm thử.

## Kiến Trúc Tổng Quan

### Sơ Đồ Kiến Trúc

```
┌─────────────────────────────────────────────────────────┐
│                    WebAPI Layer                          │
│  (Controllers, Middleware, Authentication, Filters)      │
└────────────────────┬────────────────────────────────────┘
                     │ HTTP Requests
                     ├── MediatR
                     ↓
┌─────────────────────────────────────────────────────────┐
│               Application Layer                          │
│  ┌─────────────┐  ┌──────────────┐  ┌────────────────┐ │
│  │  Commands   │  │   Queries    │  │   Behaviors    │ │
│  │  (Write)    │  │   (Read)     │  │  (Validation)  │ │
│  └─────────────┘  └──────────────┘  └────────────────┘ │
│  ┌──────────────────────────────────────────────────┐  │
│  │  DTOs, Interfaces, Common Services               │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────┬────────────────────────────────────┘
                     │ Depends on
                     ↓
┌─────────────────────────────────────────────────────────┐
│                   Domain Layer                           │
│  ┌────────────┐  ┌──────────────┐  ┌────────────────┐  │
│  │  Entities  │  │     Events   │  │   Interfaces   │  │
│  │  (Models)  │  │  (Domain)    │  │  (Repository)  │  │
│  └────────────┘  └──────────────┘  └────────────────┘  │
└─────────────────────────────────────────────────────────┘
                     ↑ Implements
                     │
┌─────────────────────────────────────────────────────────┐
│              Infrastructure Layer                        │
│  ┌────────────┐  ┌──────────────┐  ┌────────────────┐  │
│  │    EF      │  │ Repositories │  │   Services     │  │
│  │  DbContext │  │ (Data Access)│  │ (Email, Auth)  │  │
│  └────────────┘  └──────────────┘  └────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

## Các Tầng (Layers) Trong Kiến Trúc

### 1. **Domain Layer** (Tầng Trung Tâm)
   - **Vai trò**: Chứa business logic và các entities của ứng dụng
   - **Đặc điểm**: 
     - Không phụ thuộc vào bất kỳ tầng nào
     - Chứa các domain models thuần túy (POCO - Plain Old CLR Objects)
     - Định nghĩa các interfaces cho repositories
   - **Thành phần**:
     - `Entities/`: Domain models (User, Project, Permission...)
     - `Common/`: Base classes (Entity, AuditableEntity, IAggregateRoot...)
     - `Events/`: Domain events

### 2. **Application Layer** (Tầng Ứng Dụng)
   - **Vai trò**: Chứa business logic và use cases của ứng dụng
   - **Đặc điểm**:
     - Chỉ phụ thuộc vào Domain Layer
     - Triển khai CQRS pattern với MediatR
     - Validation với FluentValidation
   - **Thành phần**:
     - `Commands/`: Các lệnh thay đổi trạng thái (Create, Update, Delete)
     - `Queries/`: Các truy vấn chỉ đọc dữ liệu (Get, List)
     - `Behaviors/`: Pipeline behaviors (Validation, Logging, Transaction)
     - `Common/`: DTOs, Interfaces, Extensions

### 3. **Infrastructure Layer** (Tầng Hạ Tầng)
   - **Vai trò**: Triển khai các interfaces được định nghĩa trong Application Layer
   - **Đặc điểm**:
     - Phụ thuộc vào Domain và Application Layer
     - Xử lý việc truy cập dữ liệu, external services
   - **Thành phần**:
     - `Persistence/`: DbContext, Repositories, Migrations, Configurations
     - `Services/`: Email service, File storage, External APIs
     - `Auth/`: Authentication và Authorization services

### 4. **WebAPI Layer** (Tầng Presentation)
   - **Vai trò**: Entry point của ứng dụng, xử lý HTTP requests
   - **Đặc điểm**:
     - Phụ thuộc vào Application và Infrastructure Layer
     - Routing, Authentication, Middleware
   - **Thành phần**:
     - `Controllers/`: API endpoints
     - `Middleware/`: Custom middleware (Error handling, Logging)
     - `Filters/`: Action filters, Authorization filters
     - `Configuration/`: Swagger, CORS, JWT setup

## CQRS Pattern (Command Query Responsibility Segregation)

### Nguyên Tắc Cơ Bản

CQRS tách biệt việc **đọc** (Query) và **ghi** (Command) dữ liệu thành hai luồng riêng biệt:

#### Commands (Ghi - Write)
- Thay đổi trạng thái của hệ thống
- Không trả về dữ liệu (hoặc chỉ trả về ID/status)
- Có validation logic
- Ví dụ: `CreateProjectCommand`, `UpdateUserCommand`

#### Queries (Đọc - Read)
- Chỉ đọc dữ liệu, không thay đổi trạng thái
- Trả về DTOs
- Không có side effects
- Ví dụ: `GetProjectByIdQuery`, `GetMyProjectsQuery`

### Lợi Ích Của CQRS

1. **Separation of Concerns**: Tách biệt rõ ràng giữa read và write operations
2. **Scalability**: Có thể scale riêng phần đọc và ghi
3. **Optimization**: Tối ưu hóa queries và commands độc lập
4. **Maintainability**: Code dễ đọc, dễ bảo trì, dễ test
5. **Flexibility**: Dễ dàng thay đổi một phần mà không ảnh hưởng phần còn lại

## Luồng Xử Lý Request

### Command Flow (Ví dụ: Tạo Project)

```
1. Client → HTTP POST /api/v1/project
              ↓
2. ProjectController.CreateProject(CreateProjectCommand)
              ↓
3. Mediator.Send(command)
              ↓
4. ValidationBehavior<CreateProjectCommand>
              ↓ (nếu valid)
5. CreateProjectCommandHandler.Handle()
              ↓
6. Repository.AddAsync() → SaveChangesAsync()
              ↓
7. Return CreateProjectResponse
              ↓
8. Controller → HTTP 200 OK với response
```

### Query Flow (Ví dụ: Lấy Project)

```
1. Client → HTTP GET /api/v1/project/{id}
              ↓
2. ProjectController.GetProjectById(id)
              ↓
3. Mediator.Send(GetProjectByIdQuery)
              ↓
4. GetProjectByIdQueryHandler.Handle()
              ↓
5. DbContext.Projects.FirstOrDefaultAsync()
              ↓
6. Map to ProjectDetailDto
              ↓
7. Return ProjectDetailDto
              ↓
8. Controller → HTTP 200 OK với DTO
```

## Dependency Injection và IoC

### Nguyên Tắc Dependency Flow

```
WebAPI → Application → Domain
  ↓          ↑
Infrastructure ────┘
```

- **Domain**: Không phụ thuộc gì cả (core)
- **Application**: Chỉ phụ thuộc Domain
- **Infrastructure**: Phụ thuộc Domain và Application
- **WebAPI**: Phụ thuộc tất cả các layer

### Registration Pattern

Mỗi layer có class `Startup.cs` riêng để đăng ký services:

```csharp
// Application Layer
services.AddApplication();  // MediatR, FluentValidation, Behaviors

// Infrastructure Layer
services.AddInfrastructure(configuration);  // DbContext, Repositories, Services

// WebAPI Layer
// Controllers, Authentication, Swagger, CORS
```

## Patterns và Practices Chính

### 1. Repository Pattern
- Abstraction layer cho data access
- Interface trong Domain, implementation trong Infrastructure
- Generic repository cho common operations

### 2. Unit of Work Pattern
- DbContext đóng vai trò Unit of Work
- `SaveChangesAsync()` commit tất cả changes trong một transaction

### 3. Mediator Pattern
- MediatR library
- Loose coupling giữa controllers và handlers
- Pipeline behaviors cho cross-cutting concerns

### 4. Options Pattern
- Strongly-typed configuration
- `IOptions<T>` injection
- Ví dụ: `JwtSettings`, `FileStorageSettings`

### 5. Domain Events
- Event-driven architecture
- `DispatchDomainEventsInterceptor` trong SaveChanges
- Loose coupling giữa các aggregates

## Entity Framework Core Features

### 1. Interceptors
- `DispatchDomainEventsInterceptor`: Tự động dispatch domain events
- Audit fields (CreatedBy, ModifiedBy) tự động

### 2. Migrations
- Code-first approach
- Database schema versioning
- Seeding data

### 3. Conventions
- Snake_case naming (PostgreSQL)
- Soft delete với `DeletedOn`, `DeletedBy`
- Audit fields cho mọi entity

## Công Nghệ Sử Dụng

- **.NET 10**: Framework chính
- **Entity Framework Core 10**: ORM
- **MediatR 14**: Implement CQRS
- **FluentValidation 12**: Validation
- **PostgreSQL**: Database
- **JWT Authentication**: Security
- **Swagger/OpenAPI**: API documentation

## Kết Luận

Kiến trúc này mang lại:
- ✅ **Maintainability**: Code tổ chức rõ ràng, dễ maintain
- ✅ **Testability**: Mỗi layer có thể test độc lập
- ✅ **Scalability**: Dễ dàng scale và optimize
- ✅ **Separation of Concerns**: Mỗi layer có trách nhiệm rõ ràng
- ✅ **Flexibility**: Dễ thay đổi implementation mà không ảnh hưởng business logic

---

**Next Steps**: Xem các hướng dẫn chi tiết từng layer trong các file documentation khác.
