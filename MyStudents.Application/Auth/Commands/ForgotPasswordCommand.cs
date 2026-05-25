using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyStudents.Application.Auth.Helper;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities;

namespace MyStudents.Application.Auth.Commands;

public record ForgotPasswordCommand(string Email, string? CreatedByIp) : IRequest<bool>;

public class ForgotPasswordCommandHandler(
    IApplicationDbContext context,
    IEmailService emailService,
    IConfiguration configuration) : IRequestHandler<ForgotPasswordCommand, bool>
{
    public async Task<bool> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // 1. Tìm user theo Email
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.IsDeleted == false, cancellationToken);

        // 2. Chống User Enumeration: Nếu không tìm thấy, vẫn trả về true nhưng không làm gì tiếp
        if (user == null)
        {
            return true;
        }

        // 3. Tạo rawToken ngẫu nhiên
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var tokenHash = TokenHashHelper.HashSha256(rawToken);

        // 4. Lưu token reset vào database
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = request.CreatedByIp
        };

        context.PasswordResetTokens.Add(resetToken);
        await context.SaveChangesAsync(cancellationToken);

        // 5. Gửi email khôi phục mật khẩu
        var frontendBaseUrl = configuration["FRONTEND_BASE_URL"] ?? "http://localhost:3000";
        var resetLink = $"{frontendBaseUrl.TrimEnd('/')}/auth/reset-password?token={rawToken}";

        var subject = "[MyStudents] Yêu cầu khôi phục mật khẩu";
        var body = $@"
        <div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #f3f4f6; border-radius: 16px; background-color: #ffffff; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);"">
            <div style=""text-align: center; margin-bottom: 32px;"">
                <h2 style=""color: #10b981; margin: 0; font-size: 28px; font-weight: 800; letter-spacing: -0.025em;"">MyStudents</h2>
                <p style=""color: #6b7280; font-size: 14px; margin-top: 4px; font-weight: 500;"">Hệ thống Quản lý Lớp học Thông minh</p>
            </div>
            <div style=""color: #374151; line-height: 1.7; font-size: 16px;"">
                <p>Xin chào <strong>{user.FullName}</strong>,</p>
                <p>Chúng tôi đã nhận được yêu cầu khôi phục mật khẩu cho tài khoản của bạn tại hệ thống MyStudents.</p>
                <p>Để hoàn tất việc khôi phục mật khẩu, vui lòng click vào nút bên dưới để tiến hành đổi mật khẩu mới:</p>
                <div style=""text-align: center; margin: 36px 0;"">
                    <a href=""{resetLink}"" style=""background-color: #10b981; color: #ffffff; padding: 16px 32px; text-decoration: none; font-weight: bold; border-radius: 12px; display: inline-block; box-shadow: 0 8px 16px -4px rgba(16, 185, 129, 0.3); font-size: 16px; transition: all 0.2s ease-in-out;"">Đặt lại mật khẩu</a>
                </div>
                <p style=""font-size: 14px; color: #6b7280; background-color: #f9fafb; padding: 12px 16px; border-radius: 8px; border-left: 4px solid #10b981;"">
                    <strong>Lưu ý quan trọng:</strong><br/>
                    • Liên kết này chỉ có hiệu lực trong vòng <strong>15 phút</strong> kể từ khi email được gửi.<br/>
                    • Liên kết này chỉ có thể sử dụng được <strong>1 lần duy nhất</strong>.<br/>
                    • Nếu nút ở trên không hoạt động, bạn có thể copy link sau và dán trực tiếp vào trình duyệt: <a href=""{resetLink}"" style=""color: #10b981; word-break: break-all;"">{resetLink}</a>
                </p>
                <p style=""font-size: 13px; color: #9ca3af; margin-top: 36px; border-top: 1px solid #f3f4f6; padding-top: 16px; text-align: center;"">
                    Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này. Tài khoản của bạn vẫn an toàn.
                </p>
            </div>
        </div>";

        await emailService.SendEmailAsync(user.Email, subject, body, isHtml: true);

        return true;
    }
}
