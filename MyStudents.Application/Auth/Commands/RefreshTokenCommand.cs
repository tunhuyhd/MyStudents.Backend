using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Auth.Dto;
using MyStudents.Application.Common.Interfaces;

namespace MyStudents.Application.Auth.Commands;

public record RefreshTokenCommand(RefreshTokenRequest Request) : IRequest<AuthResponse>;

public class RefreshTokenCommandHandler(IApplicationDbContext context, IJwtService jwtService) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new Exception("Invalid or expired refresh token.");
        }

        // Generate new tokens
        var newToken = jwtService.GenerateToken(user);
        var newRefreshToken = jwtService.GenerateRefreshToken();

        // Update user record
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await context.SaveChangesAsync(cancellationToken);

        return new AuthResponse(newToken, newRefreshToken, user.Username, user.Role.Name, user.Id);
    }
}
