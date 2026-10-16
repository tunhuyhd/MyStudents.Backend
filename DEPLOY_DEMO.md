# Deploy demo: Render + Supabase Free + Vercel

Hướng dẫn dùng repo backend `tunhuyhd/MyStudents.Backend`; frontend tiếp tục chạy trên Vercel. Cấu hình miễn phí này dành cho demo/học tập; Render Free có thể sleep và Supabase Free có giới hạn. Giữ Railway hiện tại cho tới khi Render đã qua kiểm tra. Database Supabase mới sẽ trống: nếu cần dữ liệu cũ, phải export Railway PostgreSQL rồi import sang Supabase trước khi chuyển frontend.

## 1. Tạo PostgreSQL trên Supabase

1. Tạo project Supabase ở region gần người dùng/API.
2. Mở **Connect** trên project, chọn **Session pooler** và lấy connection string. Render Free có thể chạy trên IPv4; Supabase direct endpoint mặc định là IPv6, nên Session pooler tương thích hơn.
3. Lưu connection string an toàn; không commit vào GitHub. Dùng host, port, database, username, password đúng như Supabase cấp và bật SSL (`SSL Mode=Require`). Nếu dùng URI `postgresql://...`, chuyển các trường tương ứng sang định dạng Npgsql `Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require`.

## 2. Tạo Render Web Service từ Blueprint

1. Push thay đổi trong repo backend lên GitHub.
2. Trong Render chọn **New → Blueprint**, kết nối GitHub và repo backend. Render đọc `render.yaml` ở root, build bằng Dockerfile, dùng gói Free và health check `/health`.
3. Khi được hỏi các giá trị `sync: false`, nhập:

   | Key | Giá trị |
   |---|---|
   | `ConnectionStrings__DefaultConnection` | connection string Supabase dạng Npgsql, bật `SSL Mode=Require` |
   | `Cors__AllowedOrigins__0` | origin Vercel hiện tại, ví dụ `https://ten-site.vercel.app` |

   Blueprint tự sinh `JwtSettings__Key`, đặt môi trường Production và tắt local file storage. Nếu frontend có custom domain, thêm `Cors__AllowedOrigins__1` trong Render Dashboard sau khi tạo service. Chỉ nhập origin, không có path `/` cuối.

   Nếu cần demo chức năng tải ảnh đại diện, thêm `CloudinarySettings__CloudName`, `CloudinarySettings__ApiKey`, `CloudinarySettings__ApiSecret` và `CloudinarySettings__Folder=mystudents/demo/avatars` trong Render. Không dùng local storage trong container vì file sẽ mất khi service restart/sleep.

4. Deploy và chờ build hoàn tất. Kiểm tra `https://<render-service>.onrender.com/health`, kỳ vọng HTTP 200 với `{"status":"healthy"}`. Ghi lại URL API này để cấu hình Vercel.

## 3. Áp dụng schema cho database mới

API không tự chạy migration khi khởi động. Cài .NET SDK 10 và EF tool nếu cần, rồi chạy từ thư mục repo backend, với connection string Supabase trong biến môi trường:

```powershell
$env:ConnectionStrings__DefaultConnection = "<SUPABASE_POSTGRES_CONNECTION_STRING>"
dotnet tool install --global dotnet-ef --version 10.0.7
dotnet ef database update -p MyStudents.Infrastructure -s MyStudents.WebApi
```

Nếu `dotnet-ef` đã cài, bỏ qua lệnh cài. Giữ URI/database credential riêng tư. Migrations được áp dụng một lần cho database trước khi đăng nhập.

## 4. Tạo tài khoản admin demo

Bootstrapper chỉ tạo/quản lý admin khi chạy lệnh thủ công từ môi trường tin cậy; không cấu hình bootstrap password lâu dài trên Web Service public.

```powershell
$env:ConnectionStrings__DefaultConnection = "<SUPABASE_POSTGRES_CONNECTION_STRING>"
$env:JwtSettings__Key = "<SAME_RENDER_JWT_KEY>"
$env:MYSTUDENTS_BOOTSTRAP_ADMIN_PASSWORD = "<STRONG_UNIQUE_DEMO_PASSWORD>"
dotnet run --project MyStudents.WebApi -- --bootstrap-admin
Remove-Item Env:MYSTUDENTS_BOOTSTRAP_ADMIN_PASSWORD
```

Dùng email/tên admin mặc định theo `AdminBootstrapper` nếu dự án có cấu hình mặc định; đổi password ngay sau demo nếu có giao diện hỗ trợ. Không dùng password mẫu đã từng trao đổi trong repo.

## 5. Kết nối frontend Vercel hiện tại

1. Trong Vercel mở project frontend → **Settings → Environment Variables**.
2. Đặt `NEXT_PUBLIC_API_URL=https://<render-service>.onrender.com` cho Production (không thêm `/api/v1`). Biến này được nhúng lúc build.
3. Redeploy frontend để áp dụng URL API mới. Giữ URL Railway để rollback nếu cần.

## 6. Kiểm tra luồng demo

1. Mở `/health` của API, xác nhận HTTP 200.
2. Mở deployment Vercel mới, đăng nhập admin, tải danh sách học sinh và thử thao tác đọc/ghi.
3. Nếu trình duyệt báo CORS, kiểm tra `Cors__AllowedOrigins__0` khớp chính xác origin Vercel (https, domain và không có dấu `/` cuối), rồi redeploy API.
4. Nếu API trả lỗi database, kiểm tra Supabase connection string/SSL và xác nhận migrations đã được áp dụng.
5. Lần gọi đầu sau 15 phút không có traffic có thể chậm khoảng một phút do Render Free cold start. Khi xác nhận API mới hoạt động, giữ Railway thêm một thời gian trước khi xóa để dễ rollback.

## Docker local để học

```powershell
Copy-Item .env.example .env
# Sửa POSTGRES_PASSWORD và JWT_SIGNING_KEY trong .env trước khi chạy
docker compose up --build
```

Chạy migrations từ máy host tới `localhost:5432` theo README; xem trạng thái/logs bằng `docker compose ps` và `docker compose logs -f mystudents.api`. Dừng bằng `docker compose down`; giữ volume bằng cách không thêm `-v`.
