using MediatR;
using MyStudents.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyStudents.Domain.Entities.Enum;

namespace MyStudents.Application.Students.Commands;

public record UpdateStudentStatusCommand : IRequest<Unit>
{
    public Guid Id { get; init; }
    public Status Status { get; init; }
}

public class UpdateStudentStatusCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateStudentStatusCommand, Unit>
{
    public async Task<Unit> Handle(UpdateStudentStatusCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var entity = await context.Students
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.CreatedBy == userId, cancellationToken);

        if (entity == null)
            throw new MyStudents.Application.Common.Exceptions.NotFoundException("Student", request.Id);

        entity.Status = request.Status;

        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
