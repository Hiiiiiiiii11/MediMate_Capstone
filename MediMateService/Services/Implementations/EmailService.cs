using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using System;
using System.Threading.Tasks;

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
            // 1. Lấy thông tin Server & Xác thực
            var host = _config["EMAIL_HOST"] ?? "smtp-relay.brevo.com";
            var port = int.Parse(_config["EMAIL_PORT"] ?? "2525"); // Đã đổi sang 2525
            var user = _config["EMAIL_USER"];
            var pass = _config["EMAIL_PASSWORD"];

            // 2. Lấy thông tin Người gửi (Sender)
            var senderEmail = _config["SENDER_EMAIL"];
            var senderName = _config["SENDER_NAME"] ?? "MediMate System";

            // Kiểm tra cấu hình
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(senderEmail))
            {
                Console.WriteLine($"[SIMULATED EMAIL] To: {to}\nSubject: {subject}\nBody: {htmlMessage}");
                return;
            }

            try
            {
                var email = new MimeMessage();

                // Dùng SENDER_EMAIL thay vì user login
                email.From.Add(new MailboxAddress(senderName, senderEmail));
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;
                email.Body = new TextPart(TextFormat.Html) { Text = htmlMessage };

                using var smtp = new SmtpClient();

                // Kết nối
                await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);

                // Đăng nhập bằng tài khoản Login (a4be7c001...)
                await smtp.AuthenticateAsync(user, pass);

                // Gửi
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // In ra lỗi để dễ fix nếu cấu hình sai
                Console.WriteLine($"Gửi email thất bại: {ex.Message}");
                throw;
            }
        }
    }
}