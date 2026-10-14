using System.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Classroom.Services;
using MyStudents.Application.Common.Exceptions;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities;
using MyStudents.Domain.Entities.Enum;
namespace MyStudents.Application.Classroom.Commands;

public record AddStudentToClassCommand : IRequest<bool>
{
    public Guid ClassId { get; init; }
    public Guid StudentId { get; init; }
}
public class AddStudentToClassCommandHandler(IApplicationDbContext db, ClassroomService classroom) : IRequestHandler<AddStudentToClassCommand, bool>
{
    public async Task<bool> Handle(AddStudentToClassCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (!await db.Classes.AnyAsync(c => c.Id == request.ClassId, cancellationToken)) throw new NotFoundException("Class", request.ClassId);
        if (!await db.Students.AnyAsync(s => s.Id == request.StudentId, cancellationToken)) throw new NotFoundException("Student", request.StudentId);
        if (await db.ClassStudents.AnyAsync(cs => cs.ClassId == request.ClassId && cs.StudentId == request.StudentId, cancellationToken))
            throw new ConflictException("ERR_ALREADY_ENROLLED");
        db.ClassStudents.Add(new ClassStudent { ClassId = request.ClassId, StudentId = request.StudentId, JoinedAt = DateTime.UtcNow, Status = StudentClassStatus.Active });
        await classroom.SyncMembershipAsync(request.ClassId, request.StudentId, true, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}

