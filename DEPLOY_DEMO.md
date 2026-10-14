# Deploy demo: Render + Supabase Free

Hướng dẫn dùng repo backend `tunhuyhd/MyStudents.Backend` và repo frontend `tunhuyhd/MyStudents.Frontend`. Cấu hình này dành cho demo/học tập; gói miễn phí có thể sleep, quota và chính sách lưu dữ liệu có thể thay đổi. Không dùng database demo cho dữ liệu quan trọng nếu chưa có backup.

## 1. Tạo PostgreSQL trên Supabase

1. Tạo project Supabase ở region gần người dùng/API.
2. Mở **Connect** trên project, chọn **Session pooler** và lấy connection string. Render Free có thể chạy trên IPv4; Supabase direct endpoint mặc định là IPv6, nên Session pooler tương thích hơn.
3. Lưu connection string an toàn; không commit vào GitHub. Dùng host, port, database, username, password đúng như Supabase cấp và bật SSL (`SSL Mode=Require`). Nếu dùng URI `postgresql://...`, chuyển các trường tương ứng sang định dạng Npgsql `Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require`.

## 2. Tạo Render Web Service từ Dockerfile

1. Trong Render chọn **New → Web Service**, kết nối GitHub và repo backend.
2. Chọn **Docker**. Dockerfile ở root repo (`Dockerfile`); root/build context để mặc định là root.
3. Trong **Advanced → Health Check Path**, nhập `/health`. Dockerfile bind `0.0.0.0` theo biến `PORT` Render cấp.
4. Tạo các Environment Variables sau (không thêm dấu ngoặc nhọn):

   | Key | Giá trị |
   |---|---|
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `ConnectionStrings__DefaultConnection` | connection string Supabase |
   | `JwtSettings__Key` | chuỗi ngẫu nhiên bí mật tối thiểu 32 ký tự |
   | `JwtSettings__Issuer` | `MyStudentsAPI_Prod` |
   | `JwtSettings__Audience` | `MyStudentsClient_Prod` |
   | `Cors__AllowedOrigins__0` | domain Netlify, ví dụ `https://ten-site.netlify.app` |
   | `CloudinarySettings__UseLocalStorage` | `false` |
   | `CloudinarySettings__CloudName` | Cloudinary cloud name |
   | `CloudinarySettings__ApiKey` | Cloudinary API key |
   | `CloudinarySettings__ApiSecret` | Cloudinary API secret |

   Nếu demo không dùng upload ảnh, có thể bỏ các secret Cloudinary; không bật local file storage vì filesystem container không bền vững. Thêm origin thứ hai bằng `Cors__AllowedOrigins__1` nếu dùng custom domain. Chỉ nhập origin, không có path `/`.

5. Deploy và chờ build hoàn tất. Kiểm tra `https://<render-service>.onrender.com/health`, kỳ vọng HTTP 200 với `{"status":"healthy"}`.

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

## 5. Deploy frontend lên Netlify Free

1. **Add new site → Import an existing project** và chọn repo `tunhuyhd/MyStudents.Frontend`.
2. Base directory để root repo; build command `npm run build`; publish/output settings theo preset Next.js do Netlify nhận diện. Không ép static export: ứng dụng dùng Next.js runtime.
3. Thêm biến `NEXT_PUBLIC_API_URL=https://<render-service>.onrender.com` (không thêm `/api/v1`). Vì biến `NEXT_PUBLIC_*` được nhúng lúc build, đặt trước khi deploy/redeploy.
4. Deploy; sao chép domain `*.netlify.app`, thêm chính xác domain ấy vào Render `Cors__AllowedOrigins__0`, sau đó redeploy backend.

## 6. Kiểm tra luồng demo

1. Mở `/health` của API, xác nhận HTTP 200.
2. Mở site Netlify, đăng nhập admin, tải danh sách học sinh và thử một thao tác đọc/ghi.
3. Nếu trình duyệt báo CORS, kiểm tra origin chính xác (https, domain và không có dấu `/` cuối), rồi redeploy API.
4. Nếu API trả lỗi database, kiểm tra Supabase connection string/SSL và xác nhận migrations đã được áp dụng.
5. Lần gọi đầu sau thời gian nhàn rỗi có thể chậm do Render Free cold start.

## Docker local để học

```powershell
Copy-Item .env.example .env
# Sửa POSTGRES_PASSWORD và JWT_SIGNING_KEY trong .env trước khi chạy
docker compose up --build
```

Chạy migrations từ máy host tới `localhost:5432` theo README; xem trạng thái/logs bằng `docker compose ps` và `docker compose logs -f mystudents.api`. Dừng bằng `docker compose down`; giữ volume bằng cách không thêm `-v`.
