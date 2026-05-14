using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Classroom.Commands;

public record UpdateStudentInClassCommand : IRequest<bool>
{
    public Guid ClassId { get; init; }
    public Guid StudentId { get; init; }
    public StudentClassStatus Status { get; init; }
}

public class UpdateStudentInClassCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateStudentInClassCommand, bool>
{
    public async Task<bool> Handle(UpdateStudentInClassCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.ClassStudents
            .FirstOrDefaultAsync(x => x.ClassId == request.ClassId && x.StudentId == request.StudentId, cancellationToken);

        if (entity == null) return false;

        entity.Status = request.Status;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
