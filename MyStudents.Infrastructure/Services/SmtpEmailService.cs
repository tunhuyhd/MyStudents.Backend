using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using MyStudents.Application.Common.Interfaces;

namespace MyStudents.Infrastructure.Services;

public class SmtpEmailService(IConfiguration configuration) : IEmailService
{
    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        var host = configuration["SMTP_HOST"];
        var portStr = configuration["SMTP_PORT"];
        var username = configuration["SMTP_USERNAME"];
        var password = configuration["SMTP_PASSWORD"];
        var fromEmail = configuration["SMTP_FROM_EMAIL"];
        var fromName = configuration["SMTP_FROM_NAME"];

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(portStr))
        {
            // Log ra console nếu thiếu thông tin SMTP cấu hình ở môi trường Local/Development
            Console.WriteLine("=============================================================================");
            Console.WriteLine($"[SMTP SIMULATION] Cấu hình SMTP trống. Không thực hiện gửi mail thực tế.");
            Console.WriteLine($"To: {to}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Body: {body}");
            Console.WriteLine("=============================================================================");
            return;
        }

        if (!int.TryParse(portStr, out int port))
        {
            port = 587;
        }

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true
        };

        var fromAddress = new MailAddress(fromEmail ?? "no-reply@mystudents.com", fromName ?? "MyStudents");
        var toAddress = new MailAddress(to);

        using var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };

        await client.SendMailAsync(message);
    }
}
