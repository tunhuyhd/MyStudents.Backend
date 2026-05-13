using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Auth.Dto;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Constants;
using System.Threading;
using System.Threading.Tasks;

namespace MyStudents.Application.Admin.Commands;

public record AdminLoginCommand(string Username, string Password) : IRequest<AuthResponse>;

public class AdminLoginCommandHandler(
    IApplicationDbContext context, 
    IJwtService jwtService,
    IPasswordHasher passwordHasher) : IRequestHandler<AdminLoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

        // Kiểm tra user tồn tại VÀ phải có quyền Admin (không phân biệt hoa thường)
        if (user == null || !string.Equals(user.Role.Name, UserRoles.Admin, System.StringComparison.OrdinalIgnoreCase) || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new System.Exception("Invalid admin credentials.");
        }

        var token = jwtService.GenerateToken(user);
        var refreshToken = jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await context.SaveChangesAsync(cancellationToken);

        return new AuthResponse(token, refreshToken, user.Username, user.Role.Name, user.Id);
    }
}
