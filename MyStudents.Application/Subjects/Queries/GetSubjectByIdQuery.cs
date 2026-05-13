using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Application.Subjects.Dto;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Subjects.Queries;

public record GetSubjectByIdQuery(Guid Id) : IRequest<SubjectDto>;

public class GetSubjectByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetSubjectByIdQuery, SubjectDto>
{
    public async Task<SubjectDto> Handle(GetSubjectByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await context.Subjects
            .Where(s => s.Id == request.Id)
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            throw new Exception("Subject not found.");

        return result;
    }
}
