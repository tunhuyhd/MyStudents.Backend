using Microsoft.EntityFrameworkCore;
using MyStudents.Domain.Entities;

namespace MyStudents.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Teacher> Teachers { get; }
    DbSet<Class> Classes { get; }
    DbSet<Student> Students { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
