using MyStudents.Domain.Entities.Enum;

namespace MyStudents.Application.Classroom.Dto;

public class AttendanceDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? StudentEmail { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Note { get; set; }
}
