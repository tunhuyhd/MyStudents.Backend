# Hướng Dẫn WebAPI Layer

## Mục Đích

**WebAPI Layer** (Presentation Layer) là entry point của ứng dụng, chịu trách nhiệm:

- HTTP request/response handling
- Routing và endpoint definitions
- Authentication & Authorization
- Input validation (model binding)
- API documentation (Swagger)
- Middleware pipeline
- Error handling
- CORS configuration

**Nguyên tắc**: WebAPI Layer phụ thuộc vào tất cả các layers khác để wire up dependencies.

## Cấu Trúc Thư Mục

```
{{ProjectName}}.WebApi/
├── {{ProjectName}}.WebApi.csproj
├── Program.cs                          # Application entry point
├── appsettings.json                    # Configuration
├── appsettings.Development.json
├── appsettings.Production.json
├── Controllers/                        # API Controllers
│   ├── BaseApiController.cs           # Base controller
│   ├── Project/
│   │   └── ProjectController.cs
│   └── User/
│       ├── AuthController.cs
│       └── UserController.cs
├── Middleware/                         # Custom middleware
│   ├── ExceptionHandlingMiddleware.cs
│   └── RequestLoggingMiddleware.cs
├── Filters/                            # Action filters
│   └── ValidationFilter.cs
├── Attributes/                         # Custom attributes
│   └── MustBeAuthenticatedAttribute.cs
├── Configuration/                      # Configuration helpers
│   └── SwaggerConfiguration.cs
└── wwwroot/                           # Static files
```

## Dependencies

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference Application và Infrastructure -->
    <ProjectReference Include="..\{{ProjectName}}.Application\{{ProjectName}}.Application.csproj" />
    <ProjectReference Include="..\{{ProjectName}}.Infrastructure\{{ProjectName}}.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Swagger/OpenAPI -->
    <PackageReference Include="Swashbuckle.AspNetCore" Version="7.2.0" />
    
    <!-- JWT Authentication -->
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.3" />
    
    <!-- Environment variables -->
    <PackageReference Include="DotNetEnv" Version="3.1.1" />
  </ItemGroup>
</Project>
```

## Program.cs Configuration

```csharp
// Program.cs
using {{ProjectName}}.Application;
using {{ProjectName}}.Infrastructure;
using {{ProjectName}}.Infrastructure.Auth;
using {{ProjectName}}.Infrastructure.Persistence.Context;
using {{ProjectName}}.Infrastructure.Persistence.Seeder;
using {{ProjectName}}.WebApi.Configuration;
using {{ProjectName}}.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Load environment variables
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ============= Configure Services =============

// 1. Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 2. Add Controllers
builder.Services.AddControllers();

// 3. Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwtAuth();

// 4. Configure JWT Settings
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// 5. Read JWT Settings for authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.Key))
{
    throw new InvalidOperationException("JwtSettings is not properly configured.");
}

// 6. Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
        ClockSkew = TimeSpan.Zero
    };
});

// 7. Add Authorization
builder.Services.AddAuthorization();

// 8. Add Application and Infrastructure layers
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

// ============= Configure Middleware Pipeline =============

// 1. Swagger (Development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "{{ProjectName}} API v1");
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
    });
}

// 2. HTTPS Redirection
app.UseHttpsRedirection();

// 3. Static Files
app.UseStaticFiles();

// 4. Routing
app.UseRouting();

// 5. CORS
app.UseCors("AllowFrontend");

// 6. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 7. Custom Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 8. Map Controllers
app.MapControllers();

// ============= Database Migration & Seeding =============
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        await ApplicationDbSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();
```

## Controllers

### 1. Base Controller

```csharp
// Controllers/BaseApiController.cs
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

**Best Practices**:
- ✅ All controllers inherit từ BaseApiController
- ✅ Lazy initialization của MediatR
- ✅ Consistent routing pattern
- ✅ API versioning trong route

### 2. Feature Controller Example

```csharp
// Controllers/Project/ProjectController.cs
using {{ProjectName}}.Application.Projects.Commands;
using {{ProjectName}}.Application.Projects.Queries;
using {{ProjectName}}.WebApi.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace {{ProjectName}}.WebApi.Controllers.Project;

public class ProjectController : BaseApiController
{
    /// <summary>
    /// Create a new project
    /// </summary>
    [HttpPost]
    [MustBeAuthenticated]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateProject(CreateProjectCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Get all projects for current user
    /// </summary>
    [HttpGet]
    [MustBeAuthenticated]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProjects([FromQuery] GetMyProjectsQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get project by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [MustBeAuthenticated]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectById(Guid id)
    {
        var result = await Mediator.Send(new GetProjectByIdQuery { Id = id });
        return Ok(result);
    }

    /// <summary>
    /// Update project
    /// </summary>
    [HttpPut("{id:guid}")]
    [MustBeAuthenticated]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProject(
        Guid id, 
        [FromBody] UpdateProjectCommand command)
    {
        if (command == null) 
            return BadRequest();
        
        command.Id = id;
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete project
    /// </summary>
    [HttpDelete("{id:guid}")]
    [MustBeAuthenticated]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        await Mediator.Send(new DeleteProjectCommand { Id = id });
        return Ok();
    }
}
```

**Best Practices cho Controllers**:
- ✅ Thin controllers - chỉ route requests
- ✅ XML comments cho Swagger documentation
- ✅ ProducesResponseType attributes
- ✅ Proper HTTP verbs (GET, POST, PUT, DELETE)
- ✅ Route constraints (guid, int, etc.)
- ✅ Use [FromBody], [FromQuery], [FromRoute] rõ ràng
- ❌ KHÔNG có business logic trong controllers

### 3. Authentication Controller

```csharp
// Controllers/User/AuthController.cs
using {{ProjectName}}.Application.Users.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace {{ProjectName}}.WebApi.Controllers.User;

public class AuthController : BaseApiController
{
    /// <summary>
    /// Login with username and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Register new user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Login with Google OAuth
    /// </summary>
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }
}
```

## Middleware

### 1. Exception Handling Middleware

```csharp
// Middleware/ExceptionHandlingMiddleware.cs
using System.Net;
using System.Text.Json;
using {{ProjectName}}.Application.Common.Exceptions;

namespace {{ProjectName}}.WebApi.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

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
        _logger.LogError(exception, "An error occurred: {Message}", exception.Message);

        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = exception switch
        {
            ValidationException validationEx => new
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = "Validation failed",
                Errors = validationEx.Errors
            },
            NotFoundException notFoundEx => new
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Message = notFoundEx.Message,
                Errors = Array.Empty<string>()
            },
            UnauthorizedAccessException => new
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Message = "Unauthorized access",
                Errors = Array.Empty<string>()
            },
            _ => new
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An internal server error occurred",
                Errors = new[] { exception.Message }
            }
        };

        response.StatusCode = errorResponse.StatusCode;
        await response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}
```

### 2. Request Logging Middleware (Optional)

```csharp
// Middleware/RequestLoggingMiddleware.cs
namespace {{ProjectName}}.WebApi.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next, 
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation(
            "Request {Method} {Path} started", 
            context.Request.Method, 
            context.Request.Path);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        await _next(context);
        
        sw.Stop();

        _logger.LogInformation(
            "Request {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
            context.Request.Method,
            context.Request.Path,
            sw.ElapsedMilliseconds,
            context.Response.StatusCode);
    }
}
```

## Custom Attributes

### Authentication Attribute

```csharp
// Attributes/MustBeAuthenticatedAttribute.cs
using Microsoft.AspNetCore.Authorization;

namespace {{ProjectName}}.WebApi.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class MustBeAuthenticatedAttribute : AuthorizeAttribute
{
    public MustBeAuthenticatedAttribute()
    {
        // Default authentication required
    }
}
```

## Swagger Configuration

```csharp
// Configuration/SwaggerConfiguration.cs
using Microsoft.OpenApi.Models;

namespace {{ProjectName}}.WebApi.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerWithJwtAuth(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "{{ProjectName}} API",
                Version = "v1",
                Description = "Issue Tracker API with Clean Architecture and CQRS",
                Contact = new OpenApiContact
                {
                    Name = "Your Name",
                    Email = "your.email@example.com"
                }
            });

            // Add JWT Authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter 'Bearer' [space] and then your token"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Include XML comments
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }
}
```

## Configuration Files

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
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
  },
  "FileStorage": {
    "Provider": "Cloudinary",
    "MaxFileSize": 5242880
  }
}
```

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database={{ProjectName}}_dev;Username=postgres;Password=dev"
  }
}
```

### appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  }
}
```

## Environment Variables (.env)

```env
# .env file
ASPNETCORE_ENVIRONMENT=Development

# Database
DB_CONNECTION_STRING=Host=localhost;Database={{ProjectName}};Username=postgres;Password=yourpassword

# JWT
JWT_SECRET_KEY=your-super-secret-key-at-least-32-characters-long
JWT_ISSUER={{ProjectName}}API
JWT_AUDIENCE={{ProjectName}}Client

# Google OAuth
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret

# Cloudinary
CLOUDINARY_CLOUD_NAME=your-cloud-name
CLOUDINARY_API_KEY=your-api-key
CLOUDINARY_API_SECRET=your-api-secret
```

## Best Practices

### 1. Controller Design
- ✅ Thin controllers, delegate to MediatR
- ✅ Proper HTTP status codes
- ✅ XML documentation cho tất cả endpoints
- ✅ Versioned API routes
- ❌ KHÔNG có business logic

### 2. Middleware Order
- ✅ Exception handling đầu tiên
- ✅ Authentication trước Authorization
- ✅ CORS trước Authentication
- ✅ Logging middleware cuối cùng

### 3. Configuration
- ✅ Strongly-typed settings với Options pattern
- ✅ Environment-specific appsettings
- ✅ Secrets trong environment variables
- ❌ KHÔNG commit sensitive data

### 4. Error Handling
- ✅ Centralized exception handling
- ✅ Consistent error response format
- ✅ Log errors với context
- ✅ Different handling cho different exceptions

### 5. Security
- ✅ HTTPS enforcement
- ✅ CORS configuration
- ✅ JWT authentication
- ✅ Input validation
- ✅ Rate limiting (optional)

## Testing API

### Using Swagger
```
https://localhost:7001/swagger
```

### Using curl
```bash
# Login
curl -X POST https://localhost:7001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"test","password":"Test@123"}'

# Get Projects (with token)
curl -X GET https://localhost:7001/api/v1/project \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Using Postman
1. Import Swagger definition
2. Set environment variables
3. Use {{token}} for authentication

## Checklist

- [ ] Create project `{{ProjectName}}.WebApi`
- [ ] Configure Program.cs
- [ ] Add BaseApiController
- [ ] Create feature controllers
- [ ] Implement exception handling middleware
- [ ] Configure Swagger
- [ ] Setup JWT authentication
- [ ] Configure CORS
- [ ] Add appsettings files
- [ ] Setup environment variables
- [ ] Test endpoints với Swagger
- [ ] Add XML documentation
- [ ] Review security configuration
- [ ] Test error handling

---

**Next**: [Complete Setup Guide](./PROJECT_SETUP_GUIDE.md)
