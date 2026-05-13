using System.ComponentModel.DataAnnotations.Schema;
using MyStudents.Domain.Common;
using MyStudents.Domain.Constants;

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

    public static Role Admin => new Role { Name = UserRoles.Admin, Description = "Administrator with full access" };
    public static Role User => new Role { Name = UserRoles.User, Description = "Regular user with limited access" };
}
