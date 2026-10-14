using MediatR;
using MyStudents.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Subjects.Commands;

public record UpdateSubjectCommand(Guid Id, string Name, string Description) : IRequest<Unit>;

public class UpdateSubjectCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateSubjectCommand, Unit>
{
    public async Task<Unit> Handle(UpdateSubjectCommand command, CancellationToken cancellationToken)
    {
        var entity = await context.Subjects
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);

        if (entity == null)
            throw new MyStudents.Application.Common.Exceptions.NotFoundException("Subject", command.Id);

        entity.Name = command.Name;
        entity.Description = command.Description;

        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
