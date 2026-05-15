using MediatR;
using MyStudents.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Students.Commands;

public record DeleteStudentCommand : IRequest<Unit>
{
    public Guid Id { get; init; }
}

public class DeleteStudentCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<DeleteStudentCommand, Unit>
{
    public async Task<Unit> Handle(DeleteStudentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var entity = await context.Students
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.CreatedBy == userId, cancellationToken);

        if (entity == null)
            throw new Exception("Student not found or you don't have permission to delete it.");

        context.Students.Remove(entity);

        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
