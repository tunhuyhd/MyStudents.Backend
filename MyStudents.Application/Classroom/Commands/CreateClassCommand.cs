using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Classroom.Commands;

public record CreateClassCommand(string Name, string Code, CategoryOfClass Category, Guid SubjectId) : IRequest<Guid>;

public class CreateClassCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<CreateClassCommand, Guid>
{
    public async Task<Guid> Handle(CreateClassCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();

        // Check if subject exists
        var subject = await context.Subjects.AnyAsync(s => s.Id == command.SubjectId, cancellationToken);
        if (!subject) throw new Exception("Subject not found.");

        var entity = new Class
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Code = command.Code,
            CategoryOfClass = command.Category,
            SubjectId = command.SubjectId,
            TeacherId = userId
        };

        context.Classes.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
