# MyStudents Backend

REST API cho ứng dụng quản lý học sinh/lớp học, xây dựng bằng .NET 10 và PostgreSQL.

## Chạy local

Yêu cầu .NET SDK 10 và PostgreSQL 16+. Tạo database `MyStudents`, sau đó đặt connection string qua biến môi trường để không lưu thông tin đăng nhập trong source:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=MyStudents;Username=<user>;Password=<password>"
$env:JwtSettings__Key = "<secret-at-least-32-characters>"
dotnet ef database update -p MyStudents.Infrastructure -s MyStudents.WebApi
dotnet run --project MyStudents.WebApi --launch-profile http
```

API chạy tại `http://localhost:5268`; OpenAPI và Scalar chỉ được bật trong Development. CORS Development cho phép `http://localhost:3000` và `https://localhost:3000`. Môi trường khác cần đặt `Cors__AllowedOrigins__0` (và các phần tử tiếp theo) thành origin frontend chính xác. Có thể bật docs ngoài Development bằng `ApiDocs__Enabled=true`.

## Frontend

Đặt `NEXT_PUBLIC_API_URL` trỏ tới origin API, không kèm `/api/v1` (ví dụ `http://localhost:5268`), rồi chạy `npm run dev` trong `MyStudents.Frontend`.

## Database và migration

Tạo migration bằng `dotnet ef migrations add <MigrationName> -p MyStudents.Infrastructure -s MyStudents.WebApi`; áp dụng bằng `dotnet ef database update -p MyStudents.Infrastructure -s MyStudents.WebApi`. Ứng dụng không tự chạy migration khi khởi động.

## Docker Compose

Sao chép `.env.example` thành `.env`, đặt `POSTGRES_PASSWORD` mạnh, rồi chạy:

```powershell
docker compose up --build -d
```

API lắng nghe cổng `8080`; PostgreSQL lắng nghe cổng `5432`. Chạy migration từ máy host với connection string trỏ tới `localhost:5432` trước khi dùng API. Compose dùng root `Dockerfile` làm cấu hình build duy nhất.

## Kiểm thử và CI

```powershell
dotnet build MyStudents.slnx --configuration Release
dotnet test tests/MyStudents.AutomationTests/MyStudents.AutomationTests.csproj --configuration Release
```

CI GitHub Actions build và chạy bộ automation tests trên .NET 10. Không commit `.env`, thông tin đăng nhập, JWT key hoặc khóa dịch vụ.