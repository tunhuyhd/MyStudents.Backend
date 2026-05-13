using MyStudents.Application.Common.Interfaces;

namespace MyStudents.Infrastructure.Auth;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        // BCrypt tự động tạo Salt và gộp vào trong chuỗi Hash kết quả
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(password, hash);
    }
}
