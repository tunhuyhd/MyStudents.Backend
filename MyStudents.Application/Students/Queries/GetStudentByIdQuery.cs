using MediatR;
using MyStudents.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Students.Dto;

namespace MyStudents.Application.Students.Queries;

public record GetStudentByIdQuery(Guid Id) : IRequest<StudentDto>;

public class GetStudentByIdQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<GetStudentByIdQuery, StudentDto>
{
    public async Task<StudentDto> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var entity = await context.Students
            .Where(s => s.Id == request.Id && s.CreatedBy == userId)
            .Select(s => new StudentDto
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                Email = s.Email,
                DateOfBirth = s.DateOfBirth,
                Gender = s.Gender,
                School = s.School,
                ParentName = s.ParentName,
                ParentPhone = s.ParentPhone,
                Phone = s.Phone,
                Address = s.Address,
                Note = s.Note,
                Status = s.Status,
                CreatedOn = s.CreatedOn
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null)
            throw new MyStudents.Application.Common.Exceptions.NotFoundException("Student", request.Id);

        return entity;
    }
}
