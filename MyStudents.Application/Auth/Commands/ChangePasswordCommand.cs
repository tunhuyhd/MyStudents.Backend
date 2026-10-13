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
        if (!currentUserService.IsAuthenticated || userId == null)
            throw new UnauthorizedAccessException();
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 12)
            throw new MyStudents.Application.Common.Exceptions.BusinessException("ERR_PASSWORD_TOO_SHORT");
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user == null || user.Status != MyStudents.Domain.Entities.Enum.Status.Active || !passwordHasher.Verify(request.OldPassword, user.PasswordHash))
        {
            throw new MyStudents.Application.Common.Exceptions.BusinessException("ERR_INVALID_CURRENT_PASSWORD");
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.RevokeSessions();
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
