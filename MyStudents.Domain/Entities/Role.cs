using System.ComponentModel.DataAnnotations.Schema;
using MyStudents.Domain.Common;

namespace MyStudents.Domain.Entities;

[Table("roles")]
public class Role : AuditableEntity, IAggregateRoot
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();

    public Role() { }

    public static Role Admin => new Role { Name = "ADMIN", Description = "Administrator with full access" };
    public static Role User => new Role { Name = "USER", Description = "Regular user with limited access" };
}
