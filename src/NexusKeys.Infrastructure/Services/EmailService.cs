using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace GameStore.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            var smtpSection = _config.GetSection("Smtp");
            var host = smtpSection["Host"] ?? "localhost";
            var port = int.TryParse(smtpSection["Port"], out var p) ? p : 1025;
            var enableSsl = bool.TryParse(smtpSection["EnableSsl"], out var ssl) && ssl;
            var user = smtpSection["User"];
            var pass = smtpSection["Pass"];

            var message = new MailMessage();
            message.From = new MailAddress(string.IsNullOrWhiteSpace(user) ? "no-reply@nexusstore.com" : user);
            message.To.Add(new MailAddress(to));
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = string.IsNullOrWhiteSpace(user) ? CredentialCache.DefaultNetworkCredentials : new NetworkCredential(user, pass)
            };

            await client.SendMailAsync(message);
        }
    }
}
