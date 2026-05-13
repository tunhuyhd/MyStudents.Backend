using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Interfaces;

namespace MyStudents.Application.Auth.Commands;

public record ChangePasswordCommand(string OldPassword, string NewPassword) : IRequest<bool>;

public class ChangePasswordCommandHandler(
    IApplicationDbContext context, 
    ICurrentUserService currentUserService,
    IPasswordHasher passwordHasher) : IRequestHandler<ChangePasswordCommand, bool>
{
    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user == null || !passwordHasher.Verify(request.OldPassword, user.PasswordHash))
        {
            throw new Exception("Mật khẩu hiện tại không chính xác.");
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
