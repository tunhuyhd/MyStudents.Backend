using MediatR;
using MyStudents.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Students.Dto;
using MyStudents.Application.Common.Models;
using MyStudents.Domain.Entities.Enum;

namespace MyStudents.Application.Students.Queries;

public record GetStudentsQuery(int PageNumber = 1, int PageSize = 12, string? SearchTerm = null, Status? Status = null)
    : IRequest<PaginatedList<StudentDto>>;

public class GetStudentsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<GetStudentsQuery, PaginatedList<StudentDto>>
{
    public async Task<PaginatedList<StudentDto>> Handle(GetStudentsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var students = context.Students.Where(s => s.CreatedBy == userId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            students = students.Where(s =>
                (s.FirstName + " " + s.LastName).ToLower().Contains(term) ||
                s.FirstName.ToLower().Contains(term) ||
                s.LastName.ToLower().Contains(term) ||
                (s.Email != null && s.Email.ToLower().Contains(term)) ||
                (s.Phone != null && s.Phone.Contains(term)));
        }

        if (request.Status.HasValue)
            students = students.Where(s => s.Status == request.Status.Value);

        var query = students
            .OrderByDescending(s => s.CreatedOn)
            .ThenBy(s => s.Id)
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
            });

        return await PaginatedList<StudentDto>.CreateAsync(query, request.PageNumber, request.PageSize, cancellationToken);
    }
}
