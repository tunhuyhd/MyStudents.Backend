# MyStudents.AutomationTests

Dự án automation test dùng xUnit, ASP.NET Core WebApplicationFactory và SQLite in-memory. Test gọi API qua HTTP với JWT thật, chạy middleware/handler và truy vấn database quan hệ; không thay thế xác thực bằng mock.

Mỗi test có database riêng, tự tạo dữ liệu và hủy sau khi chạy. Không sử dụng PostgreSQL, Cloudinary hoặc tài khoản production trong appsettings. Không cần khởi động API riêng.

## Chạy test

Yêu cầu .NET SDK 10 và quyền tải package NuGet trong lần restore đầu tiên. Chạy từ thư mục `MyStudents.Backend`:

```powershell
dotnet restore MyStudents.slnx
dotnet test MyStudents.slnx --no-restore --logger "trx;LogFileName=automation.trx" --results-directory TestResults
```

Chạy riêng nhóm bảo mật:

```powershell
dotnet test tests/MyStudents.AutomationTests --filter FullyQualifiedName~SecurityApiTests
```

Đầu ra build ở `.artifacts/`; kết quả TRX ở `TestResults/`. Các thư mục này không được đưa vào Git. Có thể chạy test bằng Test Explorer của IDE hỗ trợ xUnit.

## Phạm vi

| Nhóm | Nội dung |
|---|---|
| SecurityApiTests | 401/403 cho API admin; cập nhật quyền/trạng thái; audit; chặn tự khóa/tự hạ quyền; dữ liệu đầu vào không hợp lệ |
| SecurityApiTests | Khóa/mở lại tài khoản thường và admin; thu hồi token khi đổi quyền, đổi mật khẩu, logout; refresh rotation và token hết hạn/rỗng |
| SessionValidationTests | JWT thiếu/sai security stamp, chữ ký sai, token hết hạn; phân quyền ở handler; chống ghi đè phiên đã thu hồi từ context cũ |
| BootstrapAndMigrationTests | Seed admin bị khóa; bootstrap với mật khẩu hợp lệ; từ chối mật khẩu ngắn và ghi đè tài khoản đã cấu hình |
| BootstrapAndMigrationTests | SQL migration vô hiệu hóa mật khẩu seed cũ nhưng bảo toàn mật khẩu đã đổi; snapshot khớp model PostgreSQL |
| OwnershipApiTests | Hai giáo viên không đọc/sửa/xóa học sinh của nhau; chủ sở hữu vẫn truy cập được; soft delete |

## CI

`.github/workflows/automation-tests.yml` chạy restore/test và lưu báo cáo TRX khi push hoặc pull request trên repository backend. Workflow mới chỉ được cấu hình trong mã nguồn; kết quả chạy CI từ xa cần được kiểm tra sau khi push.

## Mở rộng bộ test

- Thêm lớp test và dùng `ApiFactory` để tạo host/database riêng.
- Dùng `ApiFactory.RegisterAsync` và `AdminLoginAsync` để lấy token; không giả lập quyền trong API test.
- Kiểm tra cả response và dữ liệu sau thao tác, đặc biệt khi yêu cầu bị từ chối.
- Không thêm connection string hoặc thông tin đăng nhập thật vào test.

## Giới hạn

SQLite giúp test chạy độc lập nhưng không thay thế PostgreSQL khi kiểm tra khóa, transaction serializable, cạnh tranh đồng thời hoặc toàn bộ lịch sử migration. Kiểm tra snapshot PostgreSQL và SQL cập nhật dữ liệu không kết nối server PostgreSQL. Cần thêm một lượt kiểm thử trên PostgreSQL biệt lập trước khi triển khai production.

Đây là bộ test API/backend, chưa phải kiểm thử E2E giao diện bằng trình duyệt. Các nhóm lớp học, lịch và điểm danh có thể tiếp tục bổ sung vào cùng dự án.
