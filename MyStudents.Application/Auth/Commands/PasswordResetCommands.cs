using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyStudents.Application.Common.Exceptions;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities.Enum;

namespace MyStudents.Application.Auth.Commands;

public record ForgotPasswordCommand(string Email, string Language = "vi") : IRequest;
public record ValidatePasswordResetTokenQuery(string Token) : IRequest<bool>;
public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

public static class PasswordResetToken
{
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public class ForgotPasswordCommandHandler(IApplicationDbContext db, IPasswordResetEmailSender emailSender,
    TimeProvider clock, ILogger<ForgotPasswordCommandHandler> logger) : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;
        // Persist the cooldown so multiple API instances cannot send repeatedly.
        if (user == null || user.Status != Status.Active || user.PasswordResetRequestedAt > now.AddMinutes(-2)) return;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetTokenHash = PasswordResetToken.Hash(token);
        user.PasswordResetExpiresAt = now.AddMinutes(30);
        user.PasswordResetRequestedAt = now;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException) { return; }
        try
        {
            await emailSender.SendAsync(user.Email, token, request.Language, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            // Never log the reset URL, token, email address, or SMTP credentials.
            logger.LogError("Password reset email delivery failed. Verify SMTP configuration and availability.");
            // Do not reveal account existence through a different HTTP response.
        }
    }
}

public class ValidatePasswordResetTokenQueryHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<ValidatePasswordResetTokenQuery, bool>
{
    public async Task<bool> Handle(ValidatePasswordResetTokenQuery request, CancellationToken cancellationToken)
    {
        var hash = PasswordResetToken.Hash(request.Token);
        var now = clock.GetUtcNow().UtcDateTime;
        var valid = await db.Users.AnyAsync(u => u.PasswordResetTokenHash == hash &&
            u.PasswordResetExpiresAt > now && u.Status == Status.Active, cancellationToken);
        if (!valid) throw new BusinessException("ERR_INVALID_RESET_TOKEN");
        return true;
    }
}

public class ResetPasswordCommandHandler(IApplicationDbContext db, IPasswordHasher hasher, TimeProvider clock)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var hash = PasswordResetToken.Hash(request.Token);
        var now = clock.GetUtcNow().UtcDateTime;
        var user = await db.Users.SingleOrDefaultAsync(u => u.PasswordResetTokenHash == hash &&
            u.PasswordResetExpiresAt > now && u.Status == Status.Active, cancellationToken)
            ?? throw new BusinessException("ERR_INVALID_RESET_TOKEN");
        user.PasswordHash = hasher.Hash(request.NewPassword);
        user.RevokeSessions(); // also consumes the reset token; concurrency stamps enforce one-time use
        await db.SaveChangesAsync(cancellationToken);
    }
}

