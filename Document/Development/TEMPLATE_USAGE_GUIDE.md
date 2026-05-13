# Hướng Dẫn Sử Dụng Template Clean Architecture + CQRS

## 🎯 Giới Thiệu

Template này cung cấp một **cơ sở hoàn chỉnh** để tạo bất kỳ dự án ASP.NET Core nào sử dụng Clean Architecture và CQRS Pattern. Tất cả các file documentation sử dụng placeholder `{{ProjectName}}` để bạn dễ dàng thay thế bằng tên dự án của mình.

## 📋 Điều Kiện Tiên Quyết

- ✅ .NET 10 SDK hoặc cao hơn
- ✅ Visual Studio 2022 / VS Code / Rider
- ✅ PostgreSQL hoặc SQL Server (hoặc database khác)
- ✅ Git
- ✅ Hiểu biết cơ bản về C# và ASP.NET Core

## 🚀 Các Bước Sử Dụng Template

### Bước 1: Sao Chép Template

```bash
# Sao chép toàn bộ folder Documentation vào dự án mới của bạn
cp -r Documentation /path/to/your/new/project/
```

### Bước 2: Thay Thế Project Name

#### Cách 1: Sử dụng VS Code Find & Replace

1. Mở folder `Documentation` trong VS Code
2. Nhấn `Ctrl+Shift+H` (Windows/Linux) hoặc `Cmd+Shift+H` (Mac)
3. Trong ô "Find": nhập `{{ProjectName}}`
4. Trong ô "Replace": nhập tên dự án của bạn (ví dụ: `TaskManager`, `BlogSystem`, `ECommerce`)
5. Nhấn "Replace All" để thay thế trong tất cả các file

#### Cách 2: Sử dụng PowerShell (Windows)

```powershell
# Di chuyển vào folder Documentation/Development
cd YourProject/Documentation/Development

# Thay thế {{ProjectName}} bằng tên dự án của bạn
$projectName = "TaskManager"  # Thay đổi tên này
Get-ChildItem -Filter *.md | ForEach-Object {
    (Get-Content $_.FullName -Raw -Encoding UTF8) -replace '{{ProjectName}}', $projectName | 
    Set-Content $_.FullName -Encoding UTF8 -NoNewline
}
```

#### Cách 3: Sử dụng Bash (Linux/Mac)

```bash
# Di chuyển vào folder Documentation/Development
cd YourProject/Documentation/Development

# Thay thế {{ProjectName}} bằng tên dự án của bạn
projectName="TaskManager"  # Thay đổi tên này
find . -name "*.md" -type f -exec sed -i "s/{{ProjectName}}/$projectName/g" {} +
```

### Bước 3: Đọc Tài Liệu Theo Thứ Tự

Đọc các tài liệu theo thứ tự sau để hiểu rõ kiến trúc:

1. **[README.md](./README.md)** - Tổng quan về template
2. **[PROJECT_ARCHITECTURE_OVERVIEW.md](./PROJECT_ARCHITECTURE_OVERVIEW.md)** - Hiểu kiến trúc tổng quan
3. **[DOMAIN_LAYER_GUIDE.md](./DOMAIN_LAYER_GUIDE.md)** - Tạo Domain Layer
4. **[APPLICATION_LAYER_GUIDE.md](./APPLICATION_LAYER_GUIDE.md)** - Tạo Application Layer
5. **[INFRASTRUCTURE_LAYER_GUIDE.md](./INFRASTRUCTURE_LAYER_GUIDE.md)** - Tạo Infrastructure Layer
6. **[WEBAPI_LAYER_GUIDE.md](./WEBAPI_LAYER_GUIDE.md)** - Tạo WebAPI Layer
7. **[PROJECT_SETUP_GUIDE.md](./PROJECT_SETUP_GUIDE.md)** - Hướng dẫn từng bước setup
8. **[BEST_PRACTICES_AND_PATTERNS.md](./BEST_PRACTICES_AND_PATTERNS.md)** - Best practices

### Bước 4: Tạo Dự Án Theo Hướng Dẫn

Làm theo [PROJECT_SETUP_GUIDE.md](./PROJECT_SETUP_GUIDE.md) để tạo dự án từ đầu với kiến trúc Clean Architecture + CQRS.

## 📝 Ví Dụ Cụ Thể

### Ví Dụ 1: Tạo Task Management System

**Tên dự án:** `TaskManager`

**Các bước:**

1. **Thay thế placeholder:**
   ```
   {{ProjectName}} → TaskManager
   ```

2. **Cấu trúc dự án:**
   ```
   TaskManager/
   ├── TaskManager.sln
   ├── TaskManager.Domain/
   ├── TaskManager.Application/
   ├── TaskManager.Infrastructure/
   └── TaskManager.WebApi/
   ```

3. **Domain Entities:**
   - Task
   - Project  
   - User
   - TaskComment
   - TaskAssignment

4. **Application Features:**
   - Tasks/Commands/CreateTaskCommand
   - Tasks/Queries/GetTaskByIdQuery
   - Projects/Commands/CreateProjectCommand
   - Users/Queries/GetUserTasksQuery

5. **Làm theo:** [PROJECT_SETUP_GUIDE.md](./PROJECT_SETUP_GUIDE.md) nhưng thay entities bằng Task, Project, etc.

### Ví Dụ 2: Tạo Blog System

**Tên dự án:** `BlogSystem`

**Các bước:**

1. **Thay thế placeholder:**
   ```
   {{ProjectName}} → BlogSystem
   ```

2. **Cấu trúc dự án:**
   ```
   BlogSystem/
   ├── BlogSystem.sln
   ├── BlogSystem.Domain/
   ├── BlogSystem.Application/
   ├── BlogSystem.Infrastructure/
   └── BlogSystem.WebApi/
   ```

3. **Domain Entities:**
   - BlogPost
   - Category
   - Tag
   - Comment
   - Author
   - User

4. **Application Features:**
   - BlogPosts/Commands/CreateBlogPostCommand
   - BlogPosts/Commands/PublishBlogPostCommand
   - BlogPosts/Queries/GetBlogPostBySlugQuery
   - Categories/Commands/CreateCategoryCommand
   - Comments/Commands/AddCommentCommand

5. **Customize:** Thêm features đặc thù cho blog như rich text editor, SEO metadata, etc.

### Ví Dụ 3: Tạo E-Commerce System

**Tên dự án:** `ECommerceShop`

**Các bước:**

1. **Thay thế placeholder:**
   ```
   {{ProjectName}} → ECommerceShop
   ```

2. **Domain Entities:**
   - Product
   - Category
   - Order
   - OrderItem
   - Customer
   - ShoppingCart
   - Payment

3. **Application Features:**
   - Products/Commands/CreateProductCommand
   - Products/Queries/GetProductsByCategoryQuery
   - Orders/Commands/CreateOrderCommand
   - Orders/Commands/ProcessPaymentCommand
   - ShoppingCart/Commands/AddToCartCommand

## 📚 Checklist Tạo Dự Án Mới

### Phase 1: Setup Initial Structure

- [ ] Sao chép Documentation folder
- [ ] Thay thế `{{ProjectName}}` bằng tên dự án
- [ ] Đọc PROJECT_ARCHITECTURE_OVERVIEW.md
- [ ] Tạo solution (.sln)
- [ ] Tạo 4 projects (Domain, Application, Infrastructure, WebAPI)

### Phase 2: Domain Layer

- [ ] Đọc DOMAIN_LAYER_GUIDE.md
- [ ] Tạo base classes (Entity, AuditableEntity)
- [ ] Tạo repository interfaces
- [ ] Tạo domain entities với business logic
- [ ] Định nghĩa domain events (nếu cần)
- [ ] Write unit tests cho domain logic

### Phase 3: Application Layer

- [ ] Đọc APPLICATION_LAYER_GUIDE.md
- [ ] Install MediatR và FluentValidation
- [ ] Tạo Startup.cs
- [ ] Tạo IApplicationDbContext interface
- [ ] Tạo Commands với handlers
- [ ] Tạo Queries với handlers
- [ ] Add FluentValidation validators
- [ ] Implement ValidationBehavior
- [ ] Tạo DTOs
- [ ] Tạo custom exceptions

### Phase 4: Infrastructure Layer

- [ ] Đọc INFRASTRUCTURE_LAYER_GUIDE.md
- [ ] Install EF Core packages
- [ ] Implement ApplicationDbContext
- [ ] Implement repositories
- [ ] Create interceptors (audit, domain events)
- [ ] Setup authentication services (JWT)
- [ ] Configure external services
- [ ] Create initial migration
- [ ] Add seed data

### Phase 5: WebAPI Layer

- [ ] Đọc WEBAPI_LAYER_GUIDE.md
- [ ] Configure Program.cs
- [ ] Add middleware pipeline
- [ ] Create base controllers
- [ ] Create feature controllers
- [ ] Configure Swagger
- [ ] Setup appsettings.json
- [ ] Configure CORS
- [ ] Add logging

### Phase 6: Testing & Deployment

- [ ] Write unit tests
- [ ] Write integration tests
- [ ] Test API endpoints với Postman
- [ ] Setup CI/CD pipeline
- [ ] Deploy to staging
- [ ] Deploy to production

## 🎨 Customize Template

### Thay Đổi Database Provider

Template mặc định sử dụng PostgreSQL. Để đổi sang SQL Server:

```csharp
// Infrastructure/Startup.cs
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(  // Thay UseSqlServer thay vì UseNpgsql
        configuration.GetConnectionString("DefaultConnection")));
```

### Thêm External Services

Để thêm service mới (ví dụ: SMS Service):

1. **Domain/Application:** Tạo interface `ISmsService`
2. **Infrastructure:** Implement `SmsService`
3. **Infrastructure/Startup.cs:** Register service
4. **Application:** Inject và sử dụng trong handlers

### Thêm Authentication Provider

Template có JWT và Google OAuth. Để thêm Facebook/Microsoft:

1. Install package tương ứng
2. Configure trong `Program.cs`
3. Add authentication handler
4. Update appsettings.json

### Thêm Behaviors

Để thêm MediatR pipeline behavior mới (ví dụ: CachingBehavior):

1. Tạo class implement `IPipelineBehavior<TRequest, TResponse>`
2. Register trong Application/Startup.cs
3. Behavior sẽ tự động apply cho tất cả requests

## 🔍 Troubleshooting

### Lỗi: "Type or namespace not found"

**Nguyên nhân:** Chưa add project reference hoặc using statement sai.

**Giải pháp:**
```bash
# Add reference
dotnet add YourProject.Application reference YourProject.Domain
```

### Lỗi: "No service for type IApplicationDbContext"

**Nguyên nhân:** Chưa register DbContext trong DI container.

**Giải pháp:** Kiểm tra Infrastructure/Startup.cs đã được gọi trong Program.cs chưa:
```csharp
builder.Services.AddInfrastructure(builder.Configuration);
```

### Lỗi Migration: "No DbContext was found"

**Nguyên nhân:** EF Core không tìm thấy startup project.

**Giải pháp:**
```bash
dotnet ef migrations add InitialCreate --project Infrastructure --startup-project WebApi
```

### Lỗi Validation không chạy

**Nguyên nhân:** ValidationBehavior chưa được register.

**Giải pháp:** Kiểm tra Application/Startup.cs:
```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

## 📖 Best Practices Khi Sử Dụng Template

### 1. Giữ Nguyên Cấu Trúc Folder

✅ **Nên:**
```
YourProject.Application/
├── Common/
├── YourFeature1/Commands/
├── YourFeature1/Queries/
├── YourFeature2/Commands/
└── YourFeature2/Queries/
```

❌ **Không nên:**
```
YourProject.Application/
├── Commands/  # Mixed tất cả commands
├── Queries/   # Mixed tất cả queries
└── Services/  # Mixed tất cả services
```

### 2. Tuân Thủ Dependency Rule

✅ **Nên:** Domain ← Application ← Infrastructure ← WebAPI

❌ **Không:** Domain → Infrastructure (WRONG!)

### 3. Keep Domain Pure

✅ **Nên:** Business logic trong Domain entities
```csharp
public class Order
{
    public void AddItem(Product product, int quantity)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be positive");
        // Business logic
    }
}
```

❌ **Không:** Business logic trong Application handlers

### 4. DTOs vs Entities

✅ **Nên:** Return DTOs từ API
```csharp
public async Task<ProjectDto> Handle(GetProjectQuery request) { }
```

❌ **Không:** Return entities trực tiếp
```csharp
public async Task<Project> Handle(GetProjectQuery request) { } // WRONG!
```

### 5. Validation

✅ **Nên:** Use FluentValidation cho input validation
```csharp
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
    }
}
```

❌ **Không:** Validation trong controller

## 🎓 Learning Path

### Beginner (1-2 tuần)

1. Đọc và hiểu PROJECT_ARCHITECTURE_OVERVIEW.md
2. Tạo một dự án đơn giản (Todo List, Note Taking)
3. Implement 2-3 entities với CRUD operations
4. Test API với Postman

### Intermediate (2-4 tuần)

1. Implement complex business logic trong Domain
2. Add domain events
3. Implement advanced queries với filtering/pagination
4. Add authentication và authorization
5. Write unit tests và integration tests

### Advanced (1-2 tháng)

1. Implement microservices architecture
2. Add event sourcing
3. Implement CQRS với separate read/write databases
4. Add distributed caching (Redis)
5. Implement API Gateway
6. Setup CI/CD pipeline

## 📚 Tài Nguyên Bổ Sung

### Books
- **Clean Architecture** by Robert C. Martin
- **Domain-Driven Design** by Eric Evans
- **Implementing Domain-Driven Design** by Vaughn Vernon
- **Patterns, Principles, and Practices of Domain-Driven Design** by Scott Millett

### Online Resources
- [Microsoft - Clean Architecture with ASP.NET Core](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/)
- [Jason Taylor - Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture)
- [MediatR Documentation](https://github.com/jbogard/MediatR/wiki)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)

### Video Courses
- Clean Architecture with ASP.NET Core (Pluralsight)
- Domain-Driven Design Fundamentals (Pluralsight)
- CQRS in Practice (Pluralsight)

## 💡 Tips và Tricks

### Tip 1: Start Small

Bắt đầu với một feature nhỏ trước khi implement toàn bộ hệ thống. Ví dụ: chỉ làm Users management trước.

### Tip 2: Use Code Snippets

Tạo code snippets trong VS Code cho Commands, Queries, Validators để tăng tốc development:

```json
{
  "CQRS Command": {
    "prefix": "cqrs-cmd",
    "body": [
      "public record ${1:CommandName} : IRequest<${2:ResponseType}>",
      "{",
      "    $0",
      "}"
    ]
  }
}
```

### Tip 3: Database Migration Strategy

Luôn tạo migration mới thay vì sửa migration cũ trong production:

```bash
# Tạo migration mới
dotnet ef migrations add AddNewFeature

# Apply migration
dotnet ef database update
```

### Tip 4: Logging

Add logging vào critical operations:

```csharp
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);
        // ... implementation
    }
}
```

### Tip 5: Exception Handling

Sử dụng custom exceptions và handle ở middleware level:

```csharp
public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.") { }
}
```

## 🤝 Contributing

Nếu bạn phát hiện lỗi hoặc có suggestions để cải thiện template:

1. Document lại issue/suggestion
2. Tạo pull request với improvements
3. Share với community

## 📞 Hỗ Trợ

Khi gặp vấn đề:

1. **Đọc lại documentation** - 90% câu hỏi đã được trả lời
2. **Check best practices guide** - BEST_PRACTICES_AND_PATTERNS.md
3. **Debug step by step** - Sử dụng breakpoints
4. **Google the error** - Most issues đã được giải quyết
5. **Ask community** - Stack Overflow, Reddit, Discord

## 🎉 Kết Luận

Template này cung cấp một foundation mạnh mẽ để xây dựng ứng dụng ASP.NET Core với Clean Architecture và CQRS. Hãy:

- ✅ Tuân thủ architectural principles
- ✅ Giữ code clean và maintainable
- ✅ Write tests
- ✅ Document your code
- ✅ Continuous learning và improvement

**Good luck với dự án của bạn! 🚀**

---

**Tác giả:** Clean Architecture Template Team
**Ngày cập nhật:** May 2026
**Version:** 1.0.0
