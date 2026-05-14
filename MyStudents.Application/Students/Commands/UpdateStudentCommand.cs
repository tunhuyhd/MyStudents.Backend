using MediatR;
using MyStudents.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyStudents.Domain.Entities.Enum;

namespace MyStudents.Application.Students.Commands;

public record UpdateStudentCommand : IRequest<Unit>
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public Gender Gender { get; init; }
    public string? School { get; init; }
    public string? ParentName { get; init; }
    public string? ParentPhone { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? Note { get; init; }
}

public class UpdateStudentCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateStudentCommand, Unit>
{
    public async Task<Unit> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var entity = await context.Students
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.CreatedBy == userId, cancellationToken);

        if (entity == null)
            throw new Exception("Student not found or you don't have permission to update it.");

        entity.FirstName = request.FirstName;
        entity.LastName = request.LastName;
        entity.Email = request.Email;
        entity.DateOfBirth = request.DateOfBirth;
        entity.Gender = request.Gender;
        entity.School = request.School;
        entity.ParentName = request.ParentName;
        entity.ParentPhone = request.ParentPhone;
        entity.Phone = request.Phone;
        entity.Address = request.Address;
        entity.Note = request.Note;

        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
