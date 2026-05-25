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
        
        if (user == null) return false;

        user.FullName = request.FullName;
        user.Email = request.Email;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
