using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyStudents.Application.Admin.Queries;

public record GetRolesQuery : IRequest<List<RoleDto>>;

public record RoleDto(Guid Id, string Name);

public class GetRolesQueryHandler(IApplicationDbContext context) : IRequestHandler<GetRolesQuery, List<RoleDto>>
{
    public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        return await context.Roles
            .Select(r => new RoleDto(r.Id, r.Name))
            .ToListAsync(cancellationToken);
    }
}
