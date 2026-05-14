using MyStudents.Domain.Entities.Enum;

namespace MyStudents.Application.Classroom.Dto;

public class SessionDto
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Note { get; set; }
    public SessionStatus Status { get; set; }
    public int OrderIndex { get; set; }
    public int PresentCount { get; set; }
    public int TotalCount { get; set; }
}
