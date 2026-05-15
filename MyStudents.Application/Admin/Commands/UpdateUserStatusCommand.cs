using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities.Enum;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyStudents.Application.Admin.Commands;

public record UpdateUserStatusCommand(Guid UserId, Status NewStatus) : IRequest<bool>;

public class UpdateUserStatusCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateUserStatusCommand, bool>
{
    public async Task<bool> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null) return false;

        user.Status = request.NewStatus;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
