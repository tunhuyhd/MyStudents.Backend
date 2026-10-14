using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Auth.Dto;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities;
using MyStudents.Domain.Constants;

namespace MyStudents.Application.Auth.Commands;

public record RegisterUserCommand(RegisterRequest Request) : IRequest<AuthResponse>;

public class RegisterUserCommandHandler(
    IApplicationDbContext context, 
    IJwtService jwtService,
    IPasswordHasher passwordHasher) : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await context.Users.AnyAsync(u => u.Username == username, cancellationToken))
            throw new MyStudents.Application.Common.Exceptions.ConflictException("ERR_USERNAME_EXISTS");

        if (await context.Users.AnyAsync(u => u.Email.ToLower() == email, cancellationToken))
            throw new MyStudents.Application.Common.Exceptions.ConflictException("ERR_EMAIL_EXISTS");

        var userRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == UserRoles.User, cancellationToken);
        if (userRole == null) throw new Exception("User role not found.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            FullName = request.FullName,
            RoleId = userRole.Id,
            Role = userRole,
            PasswordHash = passwordHasher.Hash(request.Password),
            IsEmailVerified = false,
            CreatedBy = Guid.Empty,
            LastModifiedBy = Guid.Empty
        };

        var token = jwtService.GenerateToken(user);
        var refreshToken = jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return new AuthResponse(token, refreshToken, user.Username, userRole.Name, user.Id);
    }
}
