using MyStudents.Domain.Common;
using MyStudents.Domain.Entities.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyStudents.Domain.Entities;

[Table("class_sessions")]
public class ClassSession : AuditableEntity, IAggregateRoot
{
    [Column("class_id")]
    public Guid ClassId { get; set; }

    public Class Class { get; set; } = null!;

    [Column("schedule_id")]
    public Guid? ScheduleId { get; set; }

    public ClassSchedule? Schedule { get; set; }

    [Column("order_index")]
    public int OrderIndex { get; set; }

    [Column("date")]
    public DateOnly Date { get; set; }

    [Column("start_time")]
    public TimeOnly StartTime { get; set; }

    [Column("end_time")]
    public TimeOnly EndTime { get; set; }

    [Column("status")]
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

    [Column("note")]
    public string? Note { get; set; }

    // Navigation
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
