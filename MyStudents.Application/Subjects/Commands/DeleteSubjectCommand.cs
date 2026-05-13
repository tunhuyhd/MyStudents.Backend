using MediatR;
using MyStudents.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Subjects.Commands;

public record DeleteSubjectCommand(Guid Id) : IRequest<Unit>;

public class DeleteSubjectCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteSubjectCommand, Unit>
{
    public async Task<Unit> Handle(DeleteSubjectCommand command, CancellationToken cancellationToken)
    {
        var entity = await context.Subjects
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);

        if (entity == null)
            throw new Exception("Subject not found.");

        context.Subjects.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
