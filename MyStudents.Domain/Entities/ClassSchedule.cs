using MyStudents.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MyStudents.Domain.Entities;

[Table("class_schedules")]
public class ClassSchedule : AuditableEntity, IAggregateRoot
{
	[Column("class_id")]
	public Guid ClassId { get; set; }

	public Class Class { get; set; } = null!;

	[Column("day_of_week")]
	public DayOfWeek DayOfWeek { get; set; }

	[Column("start_time")]
	public TimeOnly StartTime { get; set; }

	// số giờ học
	[Column("duration_hours")]
	public decimal DurationHours { get; set; }
}
