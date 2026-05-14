namespace MyStudents.Application.Classroom.Dto;

public class ClassScheduleDto
{
    public Guid Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public decimal DurationHours { get; set; }
}
