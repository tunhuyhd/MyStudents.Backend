using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyStudents.Application.Admin.Queries;

public record GetUsersQuery : IRequest<List<UserDto>>;

public record UserDto(Guid Id, string Username, string Email, string FullName, string RoleName, Guid RoleId);

public class GetUsersQueryHandler(IApplicationDbContext context) : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await context.Users
            .Include(u => u.Role)
            .OrderByDescending(u => u.CreatedOn)
            .Select(u => new UserDto(
                u.Id,
                u.Username,
                u.Email,
                u.FullName,
                u.Role.Name,
                u.RoleId
            ))
            .ToListAsync(cancellationToken);
    }
}
