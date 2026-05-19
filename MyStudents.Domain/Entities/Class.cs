using System.ComponentModel.DataAnnotations.Schema;
using MyStudents.Domain.Common;
using MyStudents.Domain.Entities.Enum;

namespace MyStudents.Domain.Entities;

[Table("classes")]
public class Class : AuditableEntity, IAggregateRoot
{
	[Column("name")]
	public string Name { get; set; } = string.Empty;

	[Column("code")]
	public string Code { get; set; } = string.Empty;

	[Column("status")]
	public ClassStatus Status { get; set; } = ClassStatus.Active;

	[Column("category_of_class")]
	public CategoryOfClass CategoryOfClass { get; set; }

	[Column("subject_id")]
	public Guid SubjectId { get; set; }

	[Column("link_online")]
	public string? LinkOnline { get; set; }

	public Subject Subject { get; set; } = null!;

	[Column("teacher_id")]
	public Guid TeacherId { get; set; }

	public User Teacher { get; set; } = null!;

	// Thời gian khóa học
	[Column("start_date")]
	public DateOnly StartDate { get; set; }

	[Column("expected_end_date")]
	public DateOnly ExpectedEndDate { get; set; }

	// Navigation
	public ICollection<ClassSchedule> Schedules { get; set; } = new List<ClassSchedule>();

	public ICollection<ClassStudent> Students { get; set; } = new List<ClassStudent>();

	public ICollection<ClassSession> Sessions { get; set; } = new List<ClassSession>();

	public Class() { }
}
