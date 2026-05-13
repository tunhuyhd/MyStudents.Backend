# Hướng Dẫn Clean Architecture + CQRS Pattern với ASP.NET Core

## 📋 Về Template Này

**Template này là một cơ sở (foundation) có thể tái sử dụng để tạo bất kỳ dự án ASP.NET Core nào với Clean Architecture + CQRS.**

### Cách Sử Dụng Template

Trong toàn bộ tài liệu, bạn sẽ thấy placeholder `{{ProjectName}}` - đây là nơi bạn sẽ thay thế bằng tên dự án của mình.

**Ví dụ:**
- Nếu dự án của bạn là "TaskManager":
  - `{{ProjectName}}.Domain` → `TaskManager.Domain`
  - `{{ProjectName}}.Application` → `TaskManager.Application`
  - `namespace {{ProjectName}}.Domain.Entities` → `namespace TaskManager.Domain.Entities`

- Nếu dự án của bạn là "BlogSystem":
  - `{{ProjectName}}.Domain` → `BlogSystem.Domain`
  - `{{ProjectName}}.Application` → `BlogSystem.Application`
  - `namespace {{ProjectName}}.Domain.Entities` → `namespace BlogSystem.Domain.Entities`

**Để áp dụng cho dự án của bạn:**
1. Sao chép toàn bộ folder `Documentation`
2. Sử dụng Find & Replace (Ctrl+H) trong VS Code:
   - Tìm: `{{ProjectName}}`
   - Thay bằng: Tên dự án của bạn (ví dụ: `TaskManager`)
3. Làm theo các hướng dẫn từng bước trong tài liệu

> 📖 **Xem hướng dẫn chi tiết:** [TEMPLATE_USAGE_GUIDE.md](./TEMPLATE_USAGE_GUIDE.md)
> 
> File này chứa:
> - Hướng dẫn step-by-step sử dụng template
> - 3 ví dụ cụ thể (TaskManager, BlogSystem, ECommerce)
> - Checklist hoàn chỉnh cho từng phase
> - Troubleshooting common issues
> - Best practices và tips & tricks

---

## Giới Thiệu

Bộ tài liệu này cung cấp hướng dẫn chi tiết và toàn diện về cách xây dựng một ứng dụng ASP.NET Core sử dụng **Clean Architecture** kết hợp với **CQRS Pattern** (Command Query Responsibility Segregation).

Dự án được sử dụng làm ví dụ minh họa cho toàn bộ các khái niệm và patterns được trình bày.

## 📚 Danh Sách Tài Liệu

### 0. [📖 Hướng Dẫn Sử Dụng Template](./TEMPLATE_USAGE_GUIDE.md)

**⭐ BẮT ĐẦU TẠI ĐÂY NẾU BẠN MUỐN ÁP DỤNG TEMPLATE CHO DỰ ÁN RIÊNG!**

**Nội dung:**
- Hướng dẫn step-by-step sử dụng template
- 3 ví dụ cụ thể: TaskManager, BlogSystem, ECommerce
- Checklist hoàn chỉnh cho từng phase development
- Troubleshooting common issues
- Best practices khi customize template
- Tips & tricks để tăng tốc development
- Learning path từ beginner đến advanced

**File này là starting point để apply template vào dự án riêng của bạn!**

---

### 1. [Tổng Quan Kiến Trúc](./PROJECT_ARCHITECTURE_OVERVIEW.md)
**Nội dung:**
- Giới thiệu Clean Architecture
- Sơ đồ kiến trúc tổng quan
- Các tầng (layers) và vai trò của từng tầng
- CQRS Pattern là gì và tại sao sử dụng
- Luồng xử lý request (Command Flow và Query Flow)
- Dependency Injection và IoC
- Patterns và practices chính
- Công nghệ sử dụng

**Đọc trước tiên để hiểu tổng quan!**

### 2. [Domain Layer Guide](./DOMAIN_LAYER_GUIDE.md)
**Nội dung:**
- Mục đích và vai trò của Domain Layer
- Cấu trúc thư mục và dependencies
- Tạo base classes (Entity, AuditableEntity, IAggregateRoot)
- Repository interfaces
- Domain entities với business logic
- Domain events
- Value objects
- Best practices cho Domain Layer
- Testing domain logic

**Đây là tầng trung tâm - không phụ thuộc gì!**

### 3. [Application Layer với CQRS](./APPLICATION_LAYER_GUIDE.md)
**Nội dung:**
- Mục đích của Application Layer
- Cấu trúc và dependencies
- Triển khai CQRS Pattern:
  - Commands (Write operations)
  - Queries (Read operations)
  - Command/Query Handlers
- Validation với FluentValidation
- Pipeline behaviors
- DTOs (Data Transfer Objects)
- Custom exceptions
- Service interfaces
- Testing application layer

**Đây là nơi orchestrate business logic!**

### 4. [Infrastructure Layer Guide](./INFRASTRUCTURE_LAYER_GUIDE.md)
**Nội dung:**
- Mục đích của Infrastructure Layer
- Cấu trúc và dependencies
- Entity Framework Core implementation:
  - DbContext configuration
  - Repository implementation
  - Interceptors (Domain Events, Audit)
- Authentication services (JWT, Google OAuth)
- External services (Email, File Storage)
- Migrations
- Data seeding
- Best practices

**Implementation của tất cả infrastructure concerns!**

### 5. [WebAPI Layer Guide](./WEBAPI_LAYER_GUIDE.md)
**Nội dung:**
- Mục đích của WebAPI Layer
- Cấu trúc và dependencies
- Program.cs configuration
- Controllers và routing
- Middleware pipeline:
  - Exception handling
  - Logging
  - Authentication/Authorization
- Custom attributes
- Swagger configuration
- Configuration files (appsettings.json)
- Environment variables
- Testing API
- Best practices

**Entry point của ứng dụng!**

### 6. [Project Setup Guide - Tạo Dự Án Từ Đầu](./PROJECT_SETUP_GUIDE.md)
**Nội dung:**
- Hướng dẫn từng bước tạo dự án hoàn chỉnh
- Tạo solution và projects
- Cài đặt packages
- Implement từng layer:
  - Domain Layer
  - Application Layer
  - Infrastructure Layer
  - WebAPI Layer
- Create và apply migrations
- Run và test ứng dụng
- Git setup
- Troubleshooting

**Follow guide này để tạo dự án của bạn!**

### 7. [Best Practices và Design Patterns](./BEST_PRACTICES_AND_PATTERNS.md)
**Nội dung:**
- Clean Architecture Principles
- CQRS Pattern best practices
- Repository Pattern
- Domain-Driven Design (DDD) Patterns:
  - Aggregate Roots
  - Value Objects
  - Domain Events
- Validation patterns
- Error handling
- Entity Framework Core best practices
- Security best practices
- Performance optimization
- Testing best practices
- Summary checklists

**Reference guide cho development!**

## 🎯 Lộ Trình Học Tập

### Cho Người Mới Bắt Đầu

1. **Đọc Tổng Quan Kiến Trúc** → Hiểu big picture
2. **Domain Layer Guide** → Bắt đầu từ core
3. **Application Layer Guide** → Học CQRS
4. **Infrastructure Layer Guide** → Implementation
5. **WebAPI Layer Guide** → API endpoints
6. **Project Setup Guide** → Thực hành tạo dự án
7. **Best Practices** → Reference khi code

### Cho Developer Có Kinh Nghiệm

1. **Tổng Quan Kiến Trúc** → Quick overview
2. **Best Practices và Design Patterns** → Core concepts
3. **Project Setup Guide** → Nhanh chóng setup
4. Tham khảo các layer guides khi cần chi tiết cụ thể

## 🏗️ Kiến Trúc Tổng Quan

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
│     Commands    │    Queries    │    Behaviors          │
│      (Write)    │    (Read)     │   (Validation)        │
└────────────────────┬────────────────────────────────────┘
                     │ Depends on
                     ↓
┌─────────────────────────────────────────────────────────┐
│                   Domain Layer                           │
│    Entities     │    Events     │    Interfaces         │
└─────────────────────────────────────────────────────────┘
                     ↑ Implements
                     │
┌─────────────────────────────────────────────────────────┐
│              Infrastructure Layer                        │
│   DbContext    │  Repositories  │    Services          │
└─────────────────────────────────────────────────────────┘
```

## 🔑 Nguyên Tắc Chính

### 1. **Dependency Rule**
Dependencies chỉ point vào trong (inward), không bao giờ point ra ngoài:
- Domain → Không phụ thuộc gì
- Application → Chỉ phụ thuộc Domain
- Infrastructure → Phụ thuộc Domain và Application
- WebAPI → Phụ thuộc tất cả

### 2. **Separation of Concerns**
Mỗi layer có trách nhiệm rõ ràng và tách biệt

### 3. **CQRS Pattern**
- **Commands**: Write operations, modify state
- **Queries**: Read operations, không modify state

### 4. **Testability**
Mỗi layer có thể test độc lập

## 💻 Công Nghệ Sử Dụng

- **.NET 10**: Framework chính
- **ASP.NET Core**: Web framework
- **Entity Framework Core 10**: ORM
- **MediatR 14**: Implement CQRS pattern
- **FluentValidation 12**: Input validation
- **PostgreSQL**: Database
- **JWT**: Authentication
- **Swagger/OpenAPI**: API documentation

## 📋 Prerequisites

- .NET 10 SDK hoặc cao hơn
- Visual Studio 2022 / VS Code / JetBrains Rider
- PostgreSQL (hoặc SQL Server)
- Git
- Hiểu biết cơ bản về C#, ASP.NET Core, Entity Framework Core

## 🚀 Quick Start

```bash
# Clone repository
git clone <your-repo-url>

# Restore packages
dotnet restore

# Update database
cd {{ProjectName}}.Infrastructure
dotnet ef database update --startup-project ../{{ProjectName}}.WebApi

# Run application
cd ../{{ProjectName}}.WebApi
dotnet run

# Hoặc với watch mode
dotnet watch run
```

Truy cập: `https://localhost:7001/swagger`

## 📁 Cấu Trúc Dự Án

```
{{ProjectName}}/
├── {{ProjectName}}.Domain/              # Core business logic
│   ├── Common/                        # Base classes, interfaces
│   ├── Entities/                      # Domain models
│   └── Events/                        # Domain events
│
├── {{ProjectName}}.Application/          # Use cases, CQRS
│   ├── Common/                        # Shared components
│   ├── Projects/
│   │   ├── Commands/                  # Write operations
│   │   └── Queries/                   # Read operations
│   └── Users/
│       ├── Commands/
│       └── Queries/
│
├── {{ProjectName}}.Infrastructure/       # Data access, external services
│   ├── Persistence/
│   │   ├── Context/                   # DbContext
│   │   ├── Repositories/              # Repository implementations
│   │   └── Migrations/                # EF migrations
│   ├── Auth/                          # Authentication services
│   └── Services/                      # External services
│
└── {{ProjectName}}.WebApi/               # API endpoints
    ├── Controllers/                   # API controllers
    ├── Middleware/                    # Custom middleware
    └── Configuration/                 # Swagger, CORS, etc.
```

## 🎓 Khái Niệm Chính

### Clean Architecture
Kiến trúc tách biệt các concerns thành các layers độc lập, với dependencies pointing inward.

### CQRS (Command Query Responsibility Segregation)
Pattern tách biệt read và write operations:
- **Commands**: Thay đổi state, không return data
- **Queries**: Chỉ đọc data, không modify state

### Domain-Driven Design (DDD)
- **Entities**: Objects với identity
- **Value Objects**: Objects được định nghĩa bởi attributes
- **Aggregates**: Cluster of entities treated as one unit
- **Domain Events**: Events diễn ra trong domain

### Repository Pattern
Abstraction layer giữa domain và data access logic.

### MediatR Pattern
In-process messaging để decouple request/response và notification handling.

## 🔧 Development Workflow

### Thêm Feature Mới

1. **Domain Layer**: Tạo hoặc update entities
2. **Application Layer**: 
   - Tạo Command/Query
   - Tạo Handler
   - Tạo Validator (nếu cần)
   - Tạo DTOs
3. **Infrastructure Layer**: Update DbContext, migration (nếu cần)
4. **WebAPI Layer**: Tạo controller endpoint
5. **Test**: Unit tests và integration tests

### Adding Entity

1. Tạo entity trong Domain/Entities/
2. Implement IAggregateRoot (nếu là aggregate root)
3. Add DbSet vào ApplicationDbContext
4. Create migration
5. Update database

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test {{ProjectName}}.Application.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## 📚 Tài Liệu Tham Khảo

- [Clean Architecture - Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern - Martin Fowler](https://martinfowler.com/bliki/CQRS.html)
- [Domain-Driven Design](https://domainlanguage.com/ddd/)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)

## ❓ FAQ

### Khi nào nên sử dụng Clean Architecture?
- Dự án có quy mô trung bình đến lớn
- Cần maintainability cao
- Cần testability tốt
- Team có nhiều developers

### CQRS có phức tạp không?
CQRS thêm structure nhưng mang lại benefits:
- Code rõ ràng hơn
- Dễ optimize read/write riêng biệt
- Dễ test
- Scalable

### Có cần Domain Events không?
Domain Events optional nhưng hữu ích cho:
- Loose coupling giữa aggregates
- Side effects
- Integration events
- Audit trails

## 🤝 Contributing

Nếu bạn tìm thấy lỗi hoặc muốn cải thiện tài liệu, feel free to:
1. Create an issue
2. Submit a pull request
3. Suggest improvements

## 📝 License

This documentation is free to use for learning and reference purposes.

## 📧 Contact

Nếu có câu hỏi về tài liệu này, vui lòng tạo issue trong repository.

---

**Happy Coding! 🚀**

*Bắt đầu với [Tổng Quan Kiến Trúc](./PROJECT_ARCHITECTURE_OVERVIEW.md)*
