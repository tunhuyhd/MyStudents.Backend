using System.ComponentModel.DataAnnotations.Schema;
using MyStudents.Domain.Common;

namespace MyStudents.Domain.Entities;

[Table("teachers")]
public class Teacher : AuditableEntity, IAggregateRoot
{
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("subject")]
    public string Subject { get; set; } = string.Empty;

    public ICollection<Class> Classes { get; set; } = new List<Class>();

    public Teacher() { }
}
