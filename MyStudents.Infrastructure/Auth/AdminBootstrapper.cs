using System.Data;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Constants;
using MyStudents.Domain.Entities;
using MyStudents.Domain.Entities.Enum;
using MyStudents.Infrastructure.Persistence.Context;

namespace MyStudents.Infrastructure.Auth;

public class AdminBootstrapper(ApplicationDbContext context, IPasswordHasher passwordHasher)
{
    public async Task InitializeAsync(string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
            throw new InvalidOperationException("Bootstrap password must contain at least 12 characters.");

        await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var id = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var user = await context.Users.IgnoreQueryFilters().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        // Never reset an account that an operator has already configured.
        if (user != null && (user.IsDeleted || user.Status != Status.Inactive || !string.IsNullOrEmpty(user.PasswordHash)))
            throw new InvalidOperationException("The bootstrap administrator is already configured. Its credentials were not changed.");

        var role = await context.Roles.SingleAsync(r => r.Name == UserRoles.Admin, cancellationToken);
        if (user == null)
        {
            user = new User { Id = id, Username = "admin", Email = "admin@mystudents.com", FullName = "System Administrator", RoleId = role.Id };
            context.Users.Add(user);
        }
        user.PasswordHash = passwordHasher.Hash(password);
        user.Status = Status.Active;
        user.RoleId = role.Id;
        user.RevokeSessions();
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
