using MediatR;
using MyStudents.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Classroom.Commands;

public record UpdateSessionCommand : IRequest<bool>
{
    public Guid Id { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public string? Note { get; set; }
}

public class UpdateSessionCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateSessionCommand, bool>
{
    public async Task<bool> Handle(UpdateSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await context.ClassSessions
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (session == null) return false;

        session.Date = request.Date;
        session.StartTime = request.StartTime;
        session.EndTime = request.EndTime;
        session.Note = request.Note;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
