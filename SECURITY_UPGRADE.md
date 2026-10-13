# Nâng cấp bảo mật P0

## Thay đổi hành vi

- API quản trị yêu cầu quyền Admin; chỉ endpoint đăng nhập admin được truy cập ẩn danh.
- Handler đổi role/status kiểm tra quyền lần nữa, kiểm tra role/status hợp lệ và không cho tự hạ quyền/tự khóa tài khoản.
- Transaction serializable bao quanh việc kiểm tra admin và cập nhật để tránh hai thao tác đồng thời làm mất admin cuối cùng. Xung đột trả 409; người dùng cần tải lại dữ liệu trước khi thử lại.
- JWT chứa `security_stamp`. Mỗi request được bảo vệ kiểm tra tài khoản hiện tại, trạng thái, role và stamp trong database.
- Khóa/mở lại tài khoản, đổi role, đổi mật khẩu hoặc logout thu hồi toàn bộ phiên của tài khoản, bao gồm access token còn hạn và refresh token.
- Mật khẩu mới khi đổi hoặc bootstrap cần ít nhất 12 ký tự. Tài khoản inactive không được đăng nhập hoặc refresh.
- `POST /api/v1/auth/logout` yêu cầu Bearer token. Frontend gọi API này trước khi xóa phiên; khi không kết nối được server sẽ báo lỗi để người dùng thử lại.
- Sau đổi mật khẩu, frontend kết thúc phiên hiện tại. Người dùng cần đăng nhập lại.
- Refresh token và stamp là concurrency token của EF: một request cũ không thể khôi phục phiên đã bị thu hồi.

Hệ thống hiện lưu một refresh token cho mỗi tài khoản. Đăng nhập mới thay refresh token cũ; logout thu hồi tất cả access token của tài khoản. Chưa hỗ trợ quản lý phiên theo từng thiết bị.

## Triển khai migration

Chạy từ `MyStudents.Backend`, dùng cấu hình kết nối đúng môi trường. Sao lưu database và xem script trước khi áp dụng:

```powershell
dotnet ef migrations script AddUserImageUrl HardenAccountSessions -p MyStudents.Infrastructure -s MyStudents.WebApi -o security-upgrade.sql
dotnet ef database update -p MyStudents.Infrastructure -s MyStudents.WebApi
```

Migration thêm cột `security_stamp`, xóa refresh token cũ, và khóa tài khoản seed nếu vẫn còn đúng hash mật khẩu mặc định. Mật khẩu admin đã được thay đổi sẽ được giữ nguyên. JWT cũ không có stamp sẽ bị từ chối, nên tất cả người dùng cần đăng nhập lại.

Dừng các instance chạy phiên bản cũ khi áp dụng và chuyển sang bản mới; tránh chạy đồng thời hai phiên bản có quy tắc phiên khác nhau. Mã nguồn mới cần migration trước khi phục vụ request.

Migration không khôi phục mật khẩu mặc định khi rollback. Các migration lịch sử giữ nguyên để bảo toàn lịch sử schema; khi tạo database mới phải áp dụng đến migration mới nhất trước khi chạy API.

## Khởi tạo admin

Seed admin mới có trạng thái Inactive và không có mật khẩu sử dụng được. Không tự tạo admin hoạt động khi API khởi động.

Sau migration, dùng lệnh riêng để thiết lập mật khẩu mạnh do người vận hành chọn. Ví dụ PowerShell dưới đây không ghi mật khẩu vào lịch sử lệnh:

```powershell
$bootstrapSecret = Read-Host 'Mật khẩu admin (ít nhất 12 ký tự)' -AsSecureString
$env:MYSTUDENTS_BOOTSTRAP_ADMIN_PASSWORD = [System.Net.NetworkCredential]::new('', $bootstrapSecret).Password
try {
    dotnet run --project MyStudents.WebApi --no-launch-profile -- --bootstrap-admin
    if ($LASTEXITCODE -ne 0) { throw 'Khởi tạo admin thất bại.' }
}
finally {
    Remove-Item Env:MYSTUDENTS_BOOTSTRAP_ADMIN_PASSWORD -ErrorAction SilentlyContinue
    $bootstrapSecret.Dispose()
}
```

Lệnh chỉ kích hoạt tài khoản seed chưa cấu hình; từ chối ghi đè tài khoản đã có mật khẩu, đã hoạt động hoặc đã bị xóa. Mật khẩu không được in ra log. Không cần bắt buộc đổi mật khẩu lần đầu vì người vận hành đã tự đặt mật khẩu riêng, không có mật khẩu tạm mặc định.

Với môi trường đã triển khai, người vận hành phải áp dụng migration và khởi tạo lại tài khoản seed bị vô hiệu hóa nếu cần. Các thao tác trên database triển khai chưa được thực hiện bởi thay đổi mã nguồn này.

## Kiểm thử

```powershell
dotnet test MyStudents.slnx --logger "trx;LogFileName=automation.trx" --results-directory TestResults
```

Xem `tests/MyStudents.AutomationTests/README.md` để biết phạm vi và giới hạn kiểm thử.

Tài liệu kỹ thuật tham khảo:

- [ASP.NET Core integration tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)
- [JWT bearer events](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.jwtbearer.jwtbearerevents?view=aspnetcore-10.0)
