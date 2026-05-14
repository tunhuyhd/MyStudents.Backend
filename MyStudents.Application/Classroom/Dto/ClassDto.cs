using MyStudents.Domain.Entities.Enum;

namespace MyStudents.Application.Classroom.Dto;

public class ClassDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public ClassStatus Status { get; set; }
    public CategoryOfClass Category { get; set; }
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly ExpectedEndDate { get; set; }
    public int StudentCount { get; set; }
    public List<ClassScheduleDto> Schedules { get; set; } = new();
    public List<StudentSummaryDto> Students { get; set; } = new();
    public List<SessionDto> Sessions { get; set; } = new();
}

public class StudentSummaryDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string StudentIdNumber { get; set; } = string.Empty;
}

public class SessionDto
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public SessionStatus Status { get; set; }
    public string? Note { get; set; }
    public int PresentCount { get; set; }
    public int TotalCount { get; set; }
}
