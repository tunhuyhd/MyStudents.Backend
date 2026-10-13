using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities.Enum;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyStudents.Application.Admin.Commands;

public record UpdateUserStatusCommand(Guid UserId, Status NewStatus) : IRequest<bool>;

public class UpdateUserStatusCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser) : IRequestHandler<UpdateUserStatusCommand, bool>
{
    public async Task<bool> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        await AdminAccountGuard.EnsureAdminAsync(context, currentUser, cancellationToken);
        if (!Enum.IsDefined(request.NewStatus))
            throw new MyStudents.Application.Common.Exceptions.BusinessException("ERR_INVALID_STATUS");
        var user = await context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null) return false;

        if (user.Status == request.NewStatus) return true;
        if (user.Id == currentUser.UserId && request.NewStatus != Status.Active)
            throw new MyStudents.Application.Common.Exceptions.BusinessException("ERR_CANNOT_DISABLE_SELF");
        if (user.Role.Name == MyStudents.Domain.Constants.UserRoles.Admin && request.NewStatus != Status.Active)
            await AdminAccountGuard.EnsureAnotherAdminAsync(context, user.Id, cancellationToken);

        user.Status = request.NewStatus;
        user.RevokeSessions();
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
