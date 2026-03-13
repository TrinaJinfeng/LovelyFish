using LovelyFish.API.Server.Models;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;

using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace LovelyFish.API.Server.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;
       
        public EmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;

         
        }

        public EmailSettings Settings => _settings; // Expose settings externally

        
        public async Task<bool> SendEmail(string toEmail, string toName, string subject, string htmlContent, string textContent = null)
        {
            

            textContent ??= htmlContent; // Use HTML as fallback for plain text

            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
                email.To.Add(new MailboxAddress(toName ?? toEmail, toEmail));
                email.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = htmlContent,
                    TextBody = textContent
                };
                email.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword);
                await client.SendAsync(email);
                await client.DisconnectAsync(true);

                return true;
            }
           

            catch (System.Exception ex)
            {
                Console.WriteLine("[Zoho Email Error] " + ex.Message);
                return false;
            }
        }
        

        /// <summary>
        /// Sends emails to admin email defined in EmailSettings
        /// </summary>
        public async Task<bool> SendToAdmin(string subject, string htmlContent, string textContent = null)
        {
            return await SendEmail(_settings.AdminEmail, _settings.AdminName, subject, htmlContent, textContent);
        }
    }
}
