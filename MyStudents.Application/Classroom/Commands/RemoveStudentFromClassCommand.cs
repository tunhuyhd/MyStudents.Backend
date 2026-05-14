using MediatR;
using MyStudents.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Classroom.Commands;

public record RemoveStudentFromClassCommand : IRequest<bool>
{
    public Guid ClassId { get; init; }
    public Guid StudentId { get; init; }
}

public class RemoveStudentFromClassCommandHandler(IApplicationDbContext context) : IRequestHandler<RemoveStudentFromClassCommand, bool>
{
    public async Task<bool> Handle(RemoveStudentFromClassCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.ClassStudents
            .FirstOrDefaultAsync(x => x.ClassId == request.ClassId && x.StudentId == request.StudentId, cancellationToken);

        if (entity == null) return false;

        context.ClassStudents.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
