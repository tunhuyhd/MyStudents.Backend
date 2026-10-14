using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Classroom.Commands;
using MyStudents.Application.Common.Exceptions;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities;
using MyStudents.Domain.Entities.Enum;

namespace MyStudents.Application.Classroom.Services;

public class ClassroomService(IApplicationDbContext db, TimeProvider clock)
{
    // DateOnly/TimeOnly in the classroom API are Vietnam local wall-clock values.
    public DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime);

    public static List<ClassSession> BuildPlan(DateOnly start, DateOnly end, IReadOnlyCollection<CreateClassScheduleInput> schedules)
    {
        var ordered = schedules.OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            var previous = ordered[i - 1];
            var current = ordered[i];
            if (previous.DayOfWeek == current.DayOfWeek && previous.StartTime.AddHours((double)previous.DurationHours) > current.StartTime)
                throw new BusinessException("ERR_INTERNAL_SCHEDULE_OVERLAP", new Dictionary<string, string>
                {
                    ["day"] = current.DayOfWeek.ToString(),
                    ["time1"] = $"{previous.StartTime:HH:mm}-{previous.StartTime.AddHours((double)previous.DurationHours):HH:mm}",
                    ["time2"] = $"{current.StartTime:HH:mm}-{current.StartTime.AddHours((double)current.DurationHours):HH:mm}"
                });
        }
        var result = new List<ClassSession>();
        for (var day = start.DayNumber; day <= end.DayNumber; day++)
        {
            var date = DateOnly.FromDayNumber(day);
            foreach (var schedule in ordered.Where(s => s.DayOfWeek == date.DayOfWeek))
                result.Add(new ClassSession { Date = date, StartTime = schedule.StartTime,
                    EndTime = schedule.StartTime.AddHours((double)schedule.DurationHours), OrderIndex = result.Count + 1 });
        }
        return result;
    }

    public async Task CheckConflictsAsync(IReadOnlyCollection<ClassSession> proposed, Guid? excludeClassId,
        Guid? excludeSessionId, CancellationToken cancellationToken)
    {
        var active = proposed.Where(s => s.Status != SessionStatus.Cancelled).OrderBy(s => s.Date).ThenBy(s => s.StartTime).ToList();
        if (active.Count == 0) return;
        for (var i = 1; i < active.Count; i++)
            if (active[i - 1].Date == active[i].Date && active[i - 1].EndTime > active[i].StartTime)
                throw new ConflictException("ERR_SESSION_OVERLAP");
        var min = active[0].Date;
        var max = active[^1].Date;
        var existing = await db.ClassSessions.AsNoTracking()
            .Where(s => s.Date >= min && s.Date <= max && s.Status != SessionStatus.Cancelled && s.Class.Status != ClassStatus.Cancelled &&
                (!excludeClassId.HasValue || s.ClassId != excludeClassId.Value) && (!excludeSessionId.HasValue || s.Id != excludeSessionId.Value))
            .Select(s => new { s.Date, s.StartTime, s.EndTime }).ToListAsync(cancellationToken);
        if (active.Any(a => existing.Any(b => a.Date == b.Date && a.StartTime < b.EndTime && b.StartTime < a.EndTime)))
            throw new ConflictException("ERR_SESSION_OVERLAP");
    }

    public static string? NormalizeLink(CategoryOfClass category, string? link)
        => category != CategoryOfClass.Online || string.IsNullOrWhiteSpace(link) ? null
            : link.Trim().Contains("://") ? link.Trim() : "https://" + link.Trim();

    public async Task AddAttendancesAsync(Guid classId, IReadOnlyCollection<ClassSession> sessions, CancellationToken cancellationToken)
    {
        var members = await db.ClassStudents.Where(cs => cs.ClassId == classId && cs.Status == StudentClassStatus.Active)
            .Select(cs => new { cs.StudentId, cs.JoinedAt }).ToListAsync(cancellationToken);
        foreach (var session in sessions)
            foreach (var member in members.Where(m => DateOnly.FromDateTime(m.JoinedAt.AddHours(7)) <= session.Date))
                db.Attendances.Add(new Attendance { Session = session, StudentId = member.StudentId });
    }

    public async Task SyncMembershipAsync(Guid classId, Guid studentId, bool active, CancellationToken cancellationToken)
    {
        var today = Today;
        var rows = await db.Attendances.Include(a => a.Session).Where(a => a.StudentId == studentId && a.Session.ClassId == classId).ToListAsync(cancellationToken);
        if (!active)
        {
            db.Attendances.RemoveRange(rows.Where(a => a.Session.Date >= today && a.Session.Status == SessionStatus.Scheduled &&
                a.Status == AttendanceStatus.Pending && string.IsNullOrWhiteSpace(a.Note)));
            return;
        }
        var sessions = await db.ClassSessions.Where(s => s.ClassId == classId && s.Date >= today && s.Status == SessionStatus.Scheduled).ToListAsync(cancellationToken);
        var existingIds = rows.Select(a => a.SessionId).ToHashSet();
        foreach (var session in sessions.Where(s => !existingIds.Contains(s.Id)))
            db.Attendances.Add(new Attendance { SessionId = session.Id, StudentId = studentId });
    }

    public bool HasHistory(ClassSession session) => session.Date < Today || session.Status != SessionStatus.Scheduled ||
        session.Attendances.Any(a => a.Status != AttendanceStatus.Pending || !string.IsNullOrWhiteSpace(a.Note));
}

