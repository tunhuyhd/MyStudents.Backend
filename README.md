# MyStudents - Backend (Private Tutoring Management)

Hệ thống quản lý lớp học dành cho giáo viên, được xây dựng trên nền tảng .NET Core 10.

## 🛠 Hướng dẫn về Database (Entity Framework Core)

Mọi thao tác thay đổi cấu trúc Database phải được thực hiện thông qua **EF Core Migrations**. Đảm bảo bạn đã cài đặt công cụ bằng lệnh: `dotnet tool install --global dotnet-ef`

### 1. Tạo Migration mới
Sử dụng lệnh này sau khi bạn thay đổi các lớp Entity trong dự án `MyStudents.Domain`.

```powershell
dotnet ef migrations add <MigrationName> -p MyStudents.Infrastructure -s MyStudents.WebApi
```
*Lưu ý: Thay `<MigrationName>` bằng tên mô tả thay đổi (ví dụ: `AddClassTable`, `UpdateUserField`).*

### 2. Cập nhật Database
Áp dụng các Migration chưa chạy vào Database thực tế.

```powershell
dotnet ef database update -p MyStudents.Infrastructure -s MyStudents.WebApi
```

### 3. Xóa Migration cuối cùng
Sử dụng nếu bạn tạo nhầm Migration và **chưa** chạy lệnh `database update`.

```powershell
dotnet ef migrations remove -p MyStudents.Infrastructure -s MyStudents.WebApi
```

### 4. Kiểm tra trạng thái
Xem danh sách các Migration và trạng thái của chúng (đã chạy hay chưa).

```powershell
dotnet ef migrations list -p MyStudents.Infrastructure -s MyStudents.WebApi
```

---

## 🚀 Cấu trúc dự án

- **MyStudents.WebApi**: Lớp giao tiếp API (Startup Project).
- **MyStudents.Application**: Chứa Logic nghiệp vụ (CQRS, MediatR, DTOs).
- **MyStudents.Infrastructure**: Chứa cấu hình Database (EF Core, Context, Migrations).
- **MyStudents.Domain**: Chứa các thực thể (Entities), hằng số (Constants) và Interfaces cốt lõi.

## 🔑 Tài khoản Admin mặc định
- **Username**: `admin`
- **Password**: `admin123`
