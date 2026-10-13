using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyStudents.Application.Admin.Commands;

public record UpdateUserRoleCommand(Guid UserId, Guid NewRoleId) : IRequest<bool>;

public class UpdateUserRoleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser) : IRequestHandler<UpdateUserRoleCommand, bool>
{
    public async Task<bool> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        await AdminAccountGuard.EnsureAdminAsync(context, currentUser, cancellationToken);
        var user = await context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null) return false;

        if (!await context.Roles.AnyAsync(r => r.Id == request.NewRoleId, cancellationToken))
            throw new MyStudents.Application.Common.Exceptions.BusinessException("ERR_INVALID_ROLE");
        if (user.RoleId == request.NewRoleId) return true;
        if (user.Id == currentUser.UserId)
            throw new MyStudents.Application.Common.Exceptions.BusinessException("ERR_CANNOT_CHANGE_OWN_ROLE");
        if (user.Role.Name == MyStudents.Domain.Constants.UserRoles.Admin && user.Status == MyStudents.Domain.Entities.Enum.Status.Active)
            await AdminAccountGuard.EnsureAnotherAdminAsync(context, user.Id, cancellationToken);

        user.RoleId = request.NewRoleId;
        user.RevokeSessions();
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
