using System.ComponentModel.DataAnnotations.Schema;
using MyStudents.Domain.Common;

namespace MyStudents.Domain.Entities;

[Table("students")]
public class Student : AuditableEntity, IAggregateRoot
{
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("date_of_birth")]
    public DateTime DateOfBirth { get; set; }

    [Column("student_id_number")]
    public string StudentIdNumber { get; set; } = string.Empty;

	public ICollection<ClassStudent> Classes { get; set; } = new List<ClassStudent>();

	public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();


	public Student() { }
}
