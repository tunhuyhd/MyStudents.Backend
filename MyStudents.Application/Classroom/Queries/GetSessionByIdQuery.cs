using MediatR;
using MyStudents.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Classroom.Dto;

namespace MyStudents.Application.Classroom.Queries;

public record GetSessionByIdQuery(Guid Id) : IRequest<SessionDto?>;

public class GetSessionByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetSessionByIdQuery, SessionDto?>
{
    public async Task<SessionDto?> Handle(GetSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var session = await context.ClassSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (session == null) return null;

        return new SessionDto
        {
            Id = session.Id,
            Date = session.Date,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            Note = session.Note,
            Status = session.Status,
            ClassId = session.ClassId
        };
    }
}
