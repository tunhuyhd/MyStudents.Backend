using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using MyStudents.Domain.Common;
using MyStudents.Domain.Entities.Enum;

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
    [ConcurrencyCheck]
    public string? RefreshToken { get; set; }

    [Column("security_stamp")]
    [ConcurrencyCheck]
    public Guid SecurityStamp { get; set; } = Guid.NewGuid();

    [Column("password_reset_token_hash")]
    [ConcurrencyCheck]
    public string? PasswordResetTokenHash { get; set; }

    [Column("password_reset_expires_at")]
    public DateTime? PasswordResetExpiresAt { get; set; }

    [Column("password_reset_requested_at")]
    public DateTime? PasswordResetRequestedAt { get; set; }

    public void RevokeSessions()
    {
        SecurityStamp = Guid.NewGuid();
        RefreshToken = null;
        RefreshTokenExpiryTime = null;
        PasswordResetTokenHash = null;
        PasswordResetExpiresAt = null;
    }

    [Column("refresh_token_expiry_time")]
    public DateTime? RefreshTokenExpiryTime { get; set; }

    [Column("role_id")]
    public Guid RoleId { get; set; }

    public Role Role { get; set; } = null!;

    [Column("status")]
    public Status Status { get; set; } = Status.Active;

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    public ICollection<Class> Classes { get; set; } = new List<Class>();

}
