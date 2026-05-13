using MyStudents.Domain.Entities;

namespace MyStudents.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
}
