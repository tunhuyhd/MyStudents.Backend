using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using MyStudents.Application.Common.Interfaces;

namespace MyStudents.Infrastructure.Services;

public class PasswordResetEmailOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "";
    public string ResetPageUrl { get; set; } = "";
}

public class SmtpPasswordResetEmailSender(IOptions<PasswordResetEmailOptions> options) : IPasswordResetEmailSender
{
    public async Task SendAsync(string email, string token, string language, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.From) ||
            !Uri.TryCreate(settings.ResetPageUrl, UriKind.Absolute, out var page) ||
            (page.Scheme != Uri.UriSchemeHttps && !(page.Scheme == Uri.UriSchemeHttp && page.IsLoopback)) ||
            !string.IsNullOrEmpty(page.Query) || !string.IsNullOrEmpty(page.Fragment))
            throw new InvalidOperationException("Password reset email configuration is incomplete or invalid.");
        var url = settings.ResetPageUrl + "#token=" + Uri.EscapeDataString(token);
        var vietnamese = language == "vi";
        using var message = new MailMessage(settings.From, email)
        {
            Subject = vietnamese ? "MyStudents — Đặt lại mật khẩu" : "MyStudents — Reset your password",
            Body = vietnamese
                ? $"Để đặt lại mật khẩu, mở liên kết sau trong vòng 30 phút:\n{url}\n\nNếu bạn không yêu cầu, hãy bỏ qua email này."
                : $"To reset your password, open this link within 30 minutes:\n{url}\n\nIf you did not request this, ignore this email.",
            IsBodyHtml = false
        };
        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            UseDefaultCredentials = false
        };
        if (!string.IsNullOrWhiteSpace(settings.Username))
            client.Credentials = new NetworkCredential(settings.Username, settings.Password);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        await client.SendMailAsync(message, timeout.Token);
    }
}

