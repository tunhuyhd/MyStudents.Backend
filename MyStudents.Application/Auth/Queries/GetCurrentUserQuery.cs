using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Auth.Dto;
using MyStudents.Application.Common.Interfaces;

namespace MyStudents.Application.Auth.Queries;

public record GetCurrentUserQuery : IRequest<UserDto>;

public class GetCurrentUserQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService) : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException();
        }

        var userId = currentUserService.UserId.Value;

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new Exception("User not found.");
        }

        return new UserDto(user.Id, user.Username, user.Email, user.FullName, user.Role.Name, user.ImageUrl);
    }
}
