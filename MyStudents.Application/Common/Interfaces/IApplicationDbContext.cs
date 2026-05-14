using Microsoft.EntityFrameworkCore;
using MyStudents.Domain.Entities;

namespace MyStudents.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Class> Classes { get; }
    DbSet<Student> Students { get; }
    DbSet<Subject> Subjects { get; }
    DbSet<ClassSchedule> ClassSchedules { get; }
    DbSet<ClassSession> ClassSessions { get; }
    DbSet<ClassStudent> ClassStudents { get; }
    DbSet<Attendance> Attendances { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
