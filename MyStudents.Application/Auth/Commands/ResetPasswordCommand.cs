using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Auth.Helper;
using MyStudents.Application.Common.Interfaces;

namespace MyStudents.Application.Auth.Commands;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<bool>;

public class ResetPasswordCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher) : IRequestHandler<ResetPasswordCommand, bool>
{
    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new Exception("Mã khôi phục không hợp lệ.");
        }

        var tokenHash = TokenHashHelper.HashSha256(request.Token);

        var resetToken = await context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (resetToken == null)
        {
            throw new Exception("Mã khôi phục không hợp lệ.");
        }

        if (resetToken.UsedAt.HasValue || resetToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new Exception("Liên kết đặt lại mật khẩu đã hết hạn hoặc đã được sử dụng.");
        }

        var user = resetToken.User;
        if (user == null || user.IsDeleted)
        {
            throw new Exception("Người dùng không tồn tại hoặc đã bị xóa.");
        }

        // 1. Cập nhật mật khẩu mới
        user.PasswordHash = passwordHasher.Hash(request.NewPassword);

        // 2. Thu hồi toàn bộ Refresh Tokens để hủy phiên đăng nhập ở mọi thiết bị khác
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        // 3. Đánh dấu token đã được dùng
        resetToken.UsedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
