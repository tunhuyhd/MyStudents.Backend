using System.ComponentModel.DataAnnotations.Schema;
using MyStudents.Domain.Common;
using MyStudents.Domain.Entities.Enum;

namespace MyStudents.Domain.Entities;

[Table("students")]
public class Student : AuditableEntity, IAggregateRoot
{
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("email")]
    public string? Email { get; set; } = string.Empty;

    [Column("date_of_birth")]
    public DateTime? DateOfBirth { get; set; }

    [Column("gender")]
    public Gender Gender { get; set; }

    [Column("school")]
    public string? School { get; set; } = string.Empty;

    [Column("parent_name")]
    public string? ParentName { get; set; } = string.Empty;

    [Column("parent_phone")]
    public string? ParentPhone { get; set; } = string.Empty;

    [Column("phone")]
    public string? Phone { get; set; } = string.Empty;

    [Column("address")]
    public string? Address { get; set; } = string.Empty;

    [Column("note")]
    public string? Note { get; set; }

	public ICollection<ClassStudent> Classes { get; set; } = new List<ClassStudent>();

	public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();


	public Student() { }
}
