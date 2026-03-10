using System.Net.Mail;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;

namespace MediMateService.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlMessage);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage)
        {
            var host = _config["EMAIL_HOST"] ?? "smtp.gmail.com";
            var port = int.Parse(_config["EMAIL_PORT"] ?? "587");
            var user = _config["EMAIL_USER"]; 
            var pass = _config["EMAIL_PASSWORD"]; 

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                
                Console.WriteLine($"[SIMULATED EMAIL] To: {to}\nSubject: {subject}\nBody: {htmlMessage}");
                return;
            }

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(user));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = htmlMessage };

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(user, pass);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
