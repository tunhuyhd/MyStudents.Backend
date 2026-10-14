using System.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Classroom.Services;
using MyStudents.Application.Common.Exceptions;
using MyStudents.Application.Common.Interfaces;
namespace MyStudents.Application.Classroom.Commands;

public record RemoveStudentFromClassCommand : IRequest<bool>
{
    public Guid ClassId { get; init; }
    public Guid StudentId { get; init; }
}
public class RemoveStudentFromClassCommandHandler(IApplicationDbContext db, ClassroomService classroom) : IRequestHandler<RemoveStudentFromClassCommand, bool>
{
    public async Task<bool> Handle(RemoveStudentFromClassCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var entity = await db.ClassStudents.FirstOrDefaultAsync(cs => cs.ClassId == request.ClassId && cs.StudentId == request.StudentId, cancellationToken)
            ?? throw new NotFoundException("Enrollment", request.StudentId);
        await classroom.SyncMembershipAsync(request.ClassId, request.StudentId, false, cancellationToken);
        db.ClassStudents.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}

