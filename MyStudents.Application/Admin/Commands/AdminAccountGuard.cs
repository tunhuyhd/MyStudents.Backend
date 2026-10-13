using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Exceptions;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Constants;
using MyStudents.Domain.Entities.Enum;

namespace MyStudents.Application.Admin.Commands;

internal static class AdminAccountGuard
{
    public static async Task EnsureAdminAsync(IApplicationDbContext context, ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == null)
            throw new UnauthorizedAccessException();
        if (!await context.Users.AnyAsync(u => u.Id == currentUser.UserId &&
                u.Status == Status.Active && u.Role.Name == UserRoles.Admin, cancellationToken))
            throw new ForbiddenException();
    }

    public static async Task EnsureAnotherAdminAsync(IApplicationDbContext context, Guid targetId,
        CancellationToken cancellationToken)
    {
        if (!await context.Users.AnyAsync(u => u.Id != targetId &&
                u.Status == Status.Active && u.Role.Name == UserRoles.Admin, cancellationToken))
            throw new BusinessException("ERR_LAST_ACTIVE_ADMIN");
    }
}
