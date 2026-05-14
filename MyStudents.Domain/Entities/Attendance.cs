using MyStudents.Domain.Common;
using MyStudents.Domain.Entities.Enum;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyStudents.Domain.Entities;

[Table("attendances")]
public class Attendance : AuditableEntity, IAggregateRoot
{
    [Column("session_id")]
    public Guid SessionId { get; set; }

    public ClassSession Session { get; set; } = null!;

    [Column("student_id")]
    public Guid StudentId { get; set; }

    public Student Student { get; set; } = null!;

    [Column("status")]
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    [Column("note")]
    public string? Note { get; set; }
}
