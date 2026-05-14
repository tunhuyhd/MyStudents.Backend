using MyStudents.Domain.Entities.Enum;

namespace MyStudents.Application.Classroom.Dto;

public class ClassDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public CategoryOfClass Category { get; set; }
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly ExpectedEndDate { get; set; }
    public int StudentCount { get; set; }
    public List<ClassScheduleDto> Schedules { get; set; } = new();
}
