using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyStudents.Application.Admin.Commands;

public record UpdateUserRoleCommand(Guid UserId, Guid NewRoleId) : IRequest<bool>;

public class UpdateUserRoleCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateUserRoleCommand, bool>
{
    public async Task<bool> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null) return false;

        user.RoleId = request.NewRoleId;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
