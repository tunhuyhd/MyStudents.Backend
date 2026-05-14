using MyStudents.Domain.Entities.Enum;

namespace MyStudents.Application.Students.Dto;

public class StudentDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? School { get; set; }
    public string? ParentName { get; set; }
    public string? ParentPhone { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedOn { get; set; }
}
