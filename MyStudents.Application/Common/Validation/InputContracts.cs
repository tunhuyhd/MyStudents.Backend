using MyStudents.Application.Classroom.Commands;
using MyStudents.Domain.Entities.Enum;
namespace MyStudents.Application.Common.Validation;

public interface IStudentInput
{
    string FirstName { get; }
    string LastName { get; }
    string? Email { get; }
    DateTime? DateOfBirth { get; }
    Gender Gender { get; }
    string? School { get; }
    string? ParentName { get; }
    string? ParentPhone { get; }
    string? Phone { get; }
    string? Address { get; }
    string? Note { get; }
}
public interface IClassInput
{
    string Name { get; }
    string Code { get; }
    CategoryOfClass Category { get; }
    Guid SubjectId { get; }
    DateOnly StartDate { get; }
    DateOnly ExpectedEndDate { get; }
    string? LinkOnline { get; }
    List<CreateClassScheduleInput>? Schedules { get; }
}
public interface ISessionInput
{
    DateOnly Date { get; }
    TimeOnly StartTime { get; }
    TimeOnly EndTime { get; }
    string? Note { get; }
}

