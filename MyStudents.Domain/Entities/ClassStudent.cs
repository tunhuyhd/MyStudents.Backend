using MyStudents.Domain.Common;
using MyStudents.Domain.Entities.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MyStudents.Domain.Entities;

public class ClassStudent : AuditableEntity, IAggregateRoot
{
	[Column("class_id")]
	public Guid ClassId { get; set; }

	public Class Class { get; set; } = null!;

	[Column("student_id")]
	public Guid StudentId { get; set; }

	public Student Student { get; set; } = null!;

	[Column("joined_at")]
	public DateTime JoinedAt { get; set; }

	[Column("status")]
	public StudentClassStatus Status { get; set; }
}
