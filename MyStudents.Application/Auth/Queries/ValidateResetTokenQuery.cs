using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Auth.Helper;
using MyStudents.Application.Common.Interfaces;

namespace MyStudents.Application.Auth.Queries;

public record ValidateResetTokenQuery(string Token) : IRequest<bool>;

public class ValidateResetTokenQueryHandler(IApplicationDbContext context) : IRequestHandler<ValidateResetTokenQuery, bool>
{
    public async Task<bool> Handle(ValidateResetTokenQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return false;
        }

        var tokenHash = TokenHashHelper.HashSha256(request.Token);

        var resetToken = await context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (resetToken == null)
        {
            return false;
        }

        // Kiểm tra xem token đã dùng hoặc đã hết hạn chưa
        if (resetToken.UsedAt.HasValue || resetToken.ExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }
}
