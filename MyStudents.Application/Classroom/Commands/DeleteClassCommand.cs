using MediatR;
using MyStudents.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Classroom.Commands;

public record DeleteClassCommand(Guid Id) : IRequest<Unit>;

public class DeleteClassCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<DeleteClassCommand, Unit>
{
    public async Task<Unit> Handle(DeleteClassCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var entity = await context.Classes
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.TeacherId == userId, cancellationToken);

        if (entity == null)
            throw new MyStudents.Application.Common.Exceptions.NotFoundException("Class", command.Id);

        context.Classes.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
