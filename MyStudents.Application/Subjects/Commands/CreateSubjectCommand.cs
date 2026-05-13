using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities;

namespace MyStudents.Application.Subjects.Commands;

public record CreateSubjectCommand(string Name, string Description) : IRequest<Guid>;

public class CreateSubjectCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateSubjectCommand, Guid>
{
    public async Task<Guid> Handle(CreateSubjectCommand command, CancellationToken cancellationToken)
    {
        var entity = new Subject
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Description = command.Description
        };

        context.Subjects.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
