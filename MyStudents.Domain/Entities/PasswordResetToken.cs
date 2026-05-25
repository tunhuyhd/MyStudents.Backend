using System.ComponentModel.DataAnnotations.Schema;
using MyStudents.Domain.Common;

namespace MyStudents.Domain.Entities;

[Table("password_reset_tokens")]
public class PasswordResetToken : Entity
{
    [Column("user_id")]
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    [Column("token_hash")]
    public string TokenHash { get; set; } = string.Empty;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("used_at")]
    public DateTime? UsedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by_ip")]
    public string? CreatedByIp { get; set; }
}
