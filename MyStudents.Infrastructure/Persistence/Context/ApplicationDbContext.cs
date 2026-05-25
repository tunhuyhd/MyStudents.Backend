using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Common;
using MyStudents.Domain.Entities;
using MyStudents.Domain.Constants;

namespace MyStudents.Infrastructure.Persistence.Context;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Subject> Subjects => Set<Subject>();

	public DbSet<ClassSchedule> ClassSchedules => Set<ClassSchedule>();

	public DbSet<ClassSession> ClassSessions => Set<ClassSession>();

	public DbSet<ClassStudent> ClassStudents => Set<ClassStudent>();

	public DbSet<Attendance> Attendances => Set<Attendance>();
	public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.HasOne(d => d.Role)
                .WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure PasswordResetToken
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.Property(e => e.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed Data
        var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var fixedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = adminRoleId, Name = UserRoles.Admin, Description = "Administrator role", CreatedBy = Guid.Empty, LastModifiedBy = Guid.Empty, CreatedOn = fixedDate },
            new Role { Id = userRoleId, Name = UserRoles.User, Description = "User role", CreatedBy = Guid.Empty, LastModifiedBy = Guid.Empty, CreatedOn = fixedDate }
        );

        modelBuilder.Entity<User>().HasData(
            new User 
            { 
                Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                Username = "admin",
                Email = "admin@mystudents.com",
                FullName = "System Administrator",
                IsEmailVerified = true,
                RoleId = adminRoleId,
                PasswordHash = "$2a$11$4HIlo6ImhI/LAIXHPdIxT.YkGP.2YaLGgUbLKOtU4mkL75hRu/1v2", // Hash chuẩn xác 100% của 'admin123'
                CreatedBy = Guid.Empty,
                LastModifiedBy = Guid.Empty,
                CreatedOn = fixedDate
            }
        );

        // Global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var condition = Expression.Equal(property, Expression.Constant(false));
                var lambda = Expression.Lambda(condition, parameter);
                
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }

            // Ensure all DateTime properties are handled as UTC for PostgreSQL
            var properties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?));

            foreach (var property in properties)
            {
                property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                    v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                    v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc)));
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var now = DateTime.UtcNow;

        // Update audit fields
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = userId; 
                    entry.Entity.CreatedOn = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedBy = userId; 
                    entry.Entity.LastModifiedOn = now;
                    break;
                case EntityState.Deleted:
                    if (entry.Entity is ISoftDelete softDelete)
                    {
                        entry.State = EntityState.Modified;
                        softDelete.IsDeleted = true;
                        softDelete.DeletedBy = userId;
                        softDelete.DeletedOn = now;
                        
                        // Also update last modified
                        entry.Entity.LastModifiedBy = userId;
                        entry.Entity.LastModifiedOn = now;
                    }
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
