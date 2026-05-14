using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Application.Classroom.Dto;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Classroom.Queries;

public record GetSessionAttendancesQuery(Guid SessionId) : IRequest<List<AttendanceDto>>;

public class GetSessionAttendancesQueryHandler(IApplicationDbContext context) 
    : IRequestHandler<GetSessionAttendancesQuery, List<AttendanceDto>>
{
    public async Task<List<AttendanceDto>> Handle(GetSessionAttendancesQuery request, CancellationToken cancellationToken)
    {
        return await context.Attendances
            .Where(a => a.SessionId == request.SessionId)
            .Include(a => a.Student)
            .Select(a => new AttendanceDto
            {
                Id = a.Id,
                StudentId = a.StudentId,
                StudentName = a.Student.LastName + " " + a.Student.FirstName,
                StudentEmail = a.Student.Email,
                Status = a.Status,
                Note = a.Note
            })
            .ToListAsync(cancellationToken);
    }
}
