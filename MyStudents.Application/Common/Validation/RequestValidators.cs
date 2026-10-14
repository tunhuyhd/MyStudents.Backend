using FluentValidation;
using MyStudents.Application.Auth.Commands;
using MyStudents.Application.Auth.Dto;
using MyStudents.Application.Admin.Commands;
using MyStudents.Application.Students.Commands;
using MyStudents.Application.Classroom.Commands;
using MyStudents.Application.Classroom.Queries;
using MyStudents.Application.Subjects.Commands;

namespace MyStudents.Application.Common.Validation;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(128);
    }
}
public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator() => RuleFor(x => x.Request).NotNull().SetValidator(new RegisterRequestValidator());
}
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator() { RuleFor(x => x.Username).NotEmpty().MaximumLength(100); RuleFor(x => x.Password).NotEmpty().MaximumLength(128); }
}
public class AdminLoginCommandValidator : AbstractValidator<AdminLoginCommand>
{
    public AdminLoginCommandValidator() { RuleFor(x => x.Username).NotEmpty().MaximumLength(100); RuleFor(x => x.Password).NotEmpty().MaximumLength(128); }
}
public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator() { RuleFor(x => x.OldPassword).NotEmpty().MaximumLength(128); RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(12).MaximumLength(128); }
}
public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator() { RuleFor(x => x.FullName).NotEmpty().MaximumLength(200); RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255); }
}
public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator() { RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255); RuleFor(x => x.Language).Must(l => l is "vi" or "en"); }
}
public class ValidatePasswordResetTokenQueryValidator : AbstractValidator<ValidatePasswordResetTokenQuery>
{
    public ValidatePasswordResetTokenQueryValidator() => RuleFor(x => x.Token).NotEmpty().Matches("^[A-F0-9]{64}$");
}
public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator() { RuleFor(x => x.Token).NotEmpty().Matches("^[A-F0-9]{64}$"); RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(12).MaximumLength(128); }
}

public abstract class StudentInputValidator<T> : AbstractValidator<T> where T : IStudentInput
{
    protected StudentInputValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(255).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.DateOfBirth).Must(d => !d.HasValue || (d.Value.Year >= 1900 && d.Value.Date <= DateTime.UtcNow.Date));
        RuleFor(x => x.School).MaximumLength(200);
        RuleFor(x => x.ParentName).MaximumLength(200);
        RuleFor(x => x.ParentPhone).MaximumLength(30);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Note).MaximumLength(2000);
    }
}
public class CreateStudentCommandValidator : StudentInputValidator<CreateStudentCommand> { }
public class UpdateStudentCommandValidator : StudentInputValidator<UpdateStudentCommand>
{
    public UpdateStudentCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
public class UpdateStudentStatusCommandValidator : AbstractValidator<UpdateStudentStatusCommand>
{
    public UpdateStudentStatusCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Status).IsInEnum(); }
}

public class ClassScheduleValidator : AbstractValidator<CreateClassScheduleInput>
{
    public ClassScheduleValidator()
    {
        RuleFor(x => x.DayOfWeek).IsInEnum();
        RuleFor(x => x.DurationHours).GreaterThan(0).LessThanOrEqualTo(8)
            .Must(d => d * 60 == decimal.Truncate(d * 60)).WithMessage("Duration must be a whole number of minutes.");
        RuleFor(x => x.StartTime).Must((x, start) => (decimal)start.ToTimeSpan().TotalHours + x.DurationHours < 24)
            .WithMessage("Lessons must end before midnight on the same day.");
    }
}
public abstract class ClassInputValidator<T> : AbstractValidator<T> where T : IClassInput
{
    protected ClassInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.StartDate).Must(d => d.Year >= 1900 && d.Year < 9999);
        RuleFor(x => x.ExpectedEndDate).GreaterThanOrEqualTo(x => x.StartDate)
            .Must((x, end) => end.DayNumber - x.StartDate.DayNumber <= 366).WithMessage("A class may span at most 366 days.");
        RuleFor(x => x.Schedules).Must(s => s == null || s.Count <= 14).WithMessage("A class may have at most 14 weekly schedules.");
        RuleForEach(x => x.Schedules).NotNull().SetValidator(new ClassScheduleValidator());
        RuleFor(x => x.LinkOnline).MaximumLength(2000).Must(link => string.IsNullOrWhiteSpace(link) ||
            (Uri.TryCreate(link.Contains("://") ? link : "https://" + link, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"))
            .WithMessage("The online link must be an HTTP or HTTPS URL.");
    }
}
public class CreateClassCommandValidator : ClassInputValidator<CreateClassCommand> { }
public class UpdateClassCommandValidator : ClassInputValidator<UpdateClassCommand>
{
    public UpdateClassCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Status).IsInEnum(); }
}
public abstract class SessionInputValidator<T> : AbstractValidator<T> where T : ISessionInput
{
    protected SessionInputValidator()
    {
        RuleFor(x => x.Date).Must(d => d.Year >= 1900 && d.Year < 9999);
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime).WithMessage("End time must be after start time on the same day.");
        RuleFor(x => x.EndTime).Must((x, end) => end.ToTimeSpan() - x.StartTime.ToTimeSpan() <= TimeSpan.FromHours(8))
            .WithMessage("Lessons may last at most 8 hours.");
        RuleFor(x => x.Note).MaximumLength(2000);
    }
}
public class CreateSessionCommandValidator : SessionInputValidator<CreateSessionCommand>
{
    public CreateSessionCommandValidator() => RuleFor(x => x.ClassId).NotEmpty();
}
public class UpdateSessionCommandValidator : SessionInputValidator<UpdateSessionCommand>
{
    public UpdateSessionCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
public class UpdateAttendanceCommandValidator : AbstractValidator<UpdateAttendanceCommand>
{
    public UpdateAttendanceCommandValidator() { RuleFor(x => x.AttendanceId).NotEmpty(); RuleFor(x => x.Status).IsInEnum(); RuleFor(x => x.Note).MaximumLength(2000); }
}
public class AddStudentToClassCommandValidator : AbstractValidator<AddStudentToClassCommand>
{
    public AddStudentToClassCommandValidator() { RuleFor(x => x.ClassId).NotEmpty(); RuleFor(x => x.StudentId).NotEmpty(); }
}
public class UpdateStudentInClassCommandValidator : AbstractValidator<UpdateStudentInClassCommand>
{
    public UpdateStudentInClassCommandValidator() { RuleFor(x => x.ClassId).NotEmpty(); RuleFor(x => x.StudentId).NotEmpty(); RuleFor(x => x.Status).IsInEnum(); }
}
public class GetClassesQueryValidator : AbstractValidator<GetClassesQuery>
{
    public GetClassesQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SearchTerm).MaximumLength(200);
        RuleFor(x => x.Year).InclusiveBetween(1, 9998).When(x => x.Year.HasValue);
    }
}
public class GetClassStudentsQueryValidator : AbstractValidator<GetClassStudentsQuery>
{
    public GetClassStudentsQueryValidator() { RuleFor(x => x.ClassId).NotEmpty(); RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000000); RuleFor(x => x.PageSize).InclusiveBetween(1, 100); }
}
public class GetStudentsQueryValidator : AbstractValidator<MyStudents.Application.Students.Queries.GetStudentsQuery>
{
    public GetStudentsQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SearchTerm).MaximumLength(200);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
    }
}
public class CreateSubjectCommandValidator : AbstractValidator<CreateSubjectCommand>
{
    public CreateSubjectCommandValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(200); RuleFor(x => x.Description).MaximumLength(2000); }
}
public class UpdateSubjectCommandValidator : AbstractValidator<UpdateSubjectCommand>
{
    public UpdateSubjectCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(200); RuleFor(x => x.Description).MaximumLength(2000); }
}

