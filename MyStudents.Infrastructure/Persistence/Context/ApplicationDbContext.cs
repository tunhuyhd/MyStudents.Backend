using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Common;
using MyStudents.Domain.Entities;

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
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Student> Students => Set<Student>();

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

        // Seed Data
        var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var fixedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = adminRoleId, Name = "ADMIN", Description = "Administrator with full access", CreatedOn = fixedDate },
            new Role { Id = userRoleId, Name = "USER", Description = "Regular user with limited access", CreatedOn = fixedDate }
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
                PasswordHash = "$2a$11$R9h/lS76P5SfBvY.P3A7Pe3Zf7U5u9Z5z8w6bX.v3V5O5U5U5U5U5U5U5U", // Hash of 'admin123'
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
                var property = Expression.Property(parameter, nameof(ISoftDelete.DeletedOn));
                var condition = Expression.Equal(property, Expression.Constant(null, typeof(DateTime?)));
                var lambda = Expression.Lambda(condition, parameter);
                
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;

        // Update audit fields
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = userId; 
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedBy = userId; 
                    entry.Entity.LastModifiedOn = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
