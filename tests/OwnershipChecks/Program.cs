using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Infrastructure.Persistence.Context;
using MyStudents.Domain.Entities;

var owner = Guid.NewGuid();
var other = Guid.NewGuid();
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused").Options;
foreach (var user in new[] { new CurrentUser(owner), new CurrentUser(other), new CurrentUser(null) })
{
    using var db = new ApplicationDbContext(options, user);
    Check<Student>(db, user, new Student { CreatedBy = owner }, new Student { CreatedBy = other }, new Student { CreatedBy = owner, IsDeleted = true });
    Check<Class>(db, user, new Class { TeacherId = owner }, new Class { TeacherId = other }, new Class { TeacherId = owner, IsDeleted = true });
    var mine = new Class { TeacherId = owner };
    var theirs = new Class { TeacherId = other };
    var pupil = new Student { CreatedBy = owner };
    var foreignPupil = new Student { CreatedBy = other };
    Check<ClassSession>(db, user, new ClassSession { Class = mine }, new ClassSession { Class = theirs }, new ClassSession { Class = mine, IsDeleted = true });
    Check<ClassSchedule>(db, user, new ClassSchedule { Class = mine }, new ClassSchedule { Class = theirs }, new ClassSchedule { Class = mine, IsDeleted = true });
    Check<ClassStudent>(db, user, new ClassStudent { Class = mine, Student = pupil }, new ClassStudent { Class = theirs, Student = foreignPupil }, new ClassStudent { Class = mine, Student = foreignPupil });
    Check<Attendance>(db, user, new Attendance { Session = new ClassSession { Class = mine }, Student = pupil }, new Attendance { Session = new ClassSession { Class = theirs }, Student = foreignPupil }, new Attendance { Session = new ClassSession { Class = mine }, Student = foreignPupil });
}
Console.WriteLine("PASS: six entity filters isolate two users, reject anonymous access, soft-deleted rows and cross-owner links; all queries translate to PostgreSQL.");

void Check<T>(ApplicationDbContext db, CurrentUser user, T mine, T theirs, T hidden) where T : class
{
    var filter = db.Model.FindEntityType(typeof(T))!.GetQueryFilter()!;
    // EF caches the model: rebind its context constant to simulate per-request evaluation.
    var predicate = (Func<T, bool>)((System.Linq.Expressions.LambdaExpression)new ContextVisitor(db).Visit(filter)!).Compile();
    if (predicate(mine) != (user.UserId == owner) || predicate(theirs) != (user.UserId == other) || predicate(hidden))
        throw new Exception($"Ownership filter failed for {typeof(T).Name}, user {user.UserId}");
    var sql = db.Set<T>().ToQueryString();
    if (!sql.Contains("WHERE")) throw new Exception("Missing SQL filter");
}
sealed class CurrentUser(Guid? id) : ICurrentUserService
{
    public Guid? UserId => id;
    public string? Role => "User";
    public bool IsAuthenticated => id.HasValue;
}
sealed class ContextVisitor(ApplicationDbContext db) : System.Linq.Expressions.ExpressionVisitor
{
    protected override System.Linq.Expressions.Expression VisitConstant(System.Linq.Expressions.ConstantExpression node)
        => node.Value is ApplicationDbContext ? System.Linq.Expressions.Expression.Constant(db) : base.VisitConstant(node);
}

