using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Interfaces;

namespace MyStudents.Application.Auth.Commands;

public record UpdateProfileCommand(string FullName, string Email) : IRequest<bool>;

public class UpdateProfileCommandHandler(
    IApplicationDbContext context, 
    ICurrentUserService currentUserService) : IRequestHandler<UpdateProfileCommand, bool>
{
    public async Task<bool> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        
        if (user == null) throw new UnauthorizedAccessException();
        var email = request.Email.Trim().ToLowerInvariant();
        if (await context.Users.AnyAsync(u => u.Id != user.Id && u.Email.ToLower() == email, cancellationToken))
            throw new MyStudents.Application.Common.Exceptions.ConflictException("ERR_EMAIL_EXISTS");

        user.FullName = request.FullName;
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            user.PasswordResetTokenHash = null;
            user.PasswordResetExpiresAt = null;
            user.IsEmailVerified = false;
        }
        user.Email = email;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
