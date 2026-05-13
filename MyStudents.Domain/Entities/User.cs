using System.ComponentModel.DataAnnotations.Schema;
using MyStudents.Domain.Common;

namespace MyStudents.Domain.Entities;

[Table("users")]
public class User : AuditableEntity, IAggregateRoot
{
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("salt")]
    public string? Salt { get; set; }

    [Column("is_email_verified")]
    public bool IsEmailVerified { get; set; }

    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("refresh_token")]
    public string? RefreshToken { get; set; }

    [Column("refresh_token_expiry_time")]
    public DateTime? RefreshTokenExpiryTime { get; set; }

    [Column("role_id")]
    public Guid RoleId { get; set; }

    public Role Role { get; set; } = null!;
}
