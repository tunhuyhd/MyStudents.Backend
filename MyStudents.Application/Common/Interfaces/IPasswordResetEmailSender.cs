namespace MyStudents.Application.Common.Interfaces;

public interface IPasswordResetEmailSender
{
    Task SendAsync(string email, string token, string language, CancellationToken cancellationToken);
}

