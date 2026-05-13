using System.ComponentModel.DataAnnotations.Schema;
using MyStudents.Domain.Common;

namespace MyStudents.Domain.Entities;

[Table("classes")]
public class Class : AuditableEntity, IAggregateRoot
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Column("category_of_class")]
    public CategoryOfClass CategoryOfClass { get; set; }

    [Column("subject_id")]
	public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

	[Column("teacher_id")]
    public Guid TeacherId { get; set; }
    public User Teacher { get; set; } = null!;

    public ICollection<Student> Students { get; set; } = new List<Student>();

    public Class() { }
}
