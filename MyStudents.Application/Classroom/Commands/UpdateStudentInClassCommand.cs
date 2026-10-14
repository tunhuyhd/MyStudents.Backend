using System.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Classroom.Services;
using MyStudents.Application.Common.Exceptions;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities.Enum;
namespace MyStudents.Application.Classroom.Commands;

public record UpdateStudentInClassCommand : IRequest<bool>
{
    public Guid ClassId { get; init; }
    public Guid StudentId { get; init; }
    public StudentClassStatus Status { get; init; }
}
public class UpdateStudentInClassCommandHandler(IApplicationDbContext db, ClassroomService classroom) : IRequestHandler<UpdateStudentInClassCommand, bool>
{
    public async Task<bool> Handle(UpdateStudentInClassCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var entity = await db.ClassStudents.FirstOrDefaultAsync(cs => cs.ClassId == request.ClassId && cs.StudentId == request.StudentId, cancellationToken)
            ?? throw new NotFoundException("Enrollment", request.StudentId);
        entity.Status = request.Status;
        await classroom.SyncMembershipAsync(request.ClassId, request.StudentId, request.Status == StudentClassStatus.Active, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}

