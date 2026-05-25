using System.Security.Cryptography;
using System.Text;

namespace MyStudents.Application.Auth.Helper;

public static class TokenHashHelper
{
    public static string HashSha256(string token)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
