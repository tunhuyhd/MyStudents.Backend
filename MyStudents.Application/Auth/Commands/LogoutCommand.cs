using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Interfaces;

namespace MyStudents.Application.Auth.Commands;

public record LogoutCommand : IRequest;

public class LogoutCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == null)
            throw new UnauthorizedAccessException();
        var user = await context.Users.SingleOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException();
        user.RevokeSessions();
        await context.SaveChangesAsync(cancellationToken);
    }
}
