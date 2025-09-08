using LovelyFish.API.Server.Models;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LovelyFish.API.Server.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;
        private readonly HttpClient _httpClient;

        // Constructor injects EmailSettings and initializes HttpClient
        public EmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("api-key", _settings.BrevoApiKey);
        }

        public EmailSettings Settings => _settings; // Expose settings externally

        /// <summary>
        /// Sends an email using Brevo (Sendinblue) SMTP API
        /// </summary>
        /// <param name="toEmail">Recipient email</param>
        /// <param name="toName">Recipient name</param>
        /// <param name="subject">Email subject</param>
        /// <param name="htmlContent">HTML content</param>
        /// <param name="textContent">Plain text content (optional)</param>
        /// <returns>True if email sent successfully, otherwise false</returns>
        public async Task<bool> SendEmail(string toEmail, string toName, string subject, string htmlContent, string textContent = null)
        {
            if (string.IsNullOrEmpty(_settings.BrevoApiKey))
                return false;

            textContent ??= htmlContent; // Use HTML as fallback for plain text

            try
            {
                var payload = new
                {
                    sender = new { email = _settings.FromEmail, name = _settings.FromName },
                    to = new[] { new { email = toEmail, name = toName } },
                    subject,
                    htmlContent,
                    textContent
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", content);

                return response.IsSuccessStatusCode;
            }
            catch
            {
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
