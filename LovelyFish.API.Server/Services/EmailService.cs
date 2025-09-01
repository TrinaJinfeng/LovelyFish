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

        public EmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("api-key", _settings.BrevoApiKey);
        }

        public EmailSettings Settings => _settings; // 提供外部访问配置的属性

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="toEmail">收件人邮箱</param>
        /// <param name="toName">收件人姓名</param>
        /// <param name="subject">邮件主题</param>
        /// <param name="htmlContent">HTML 内容</param>
        /// <param name="textContent">纯文本内容（可选）</param>
        /// <returns>发送成功返回 true，否则 false</returns>
        public async Task<bool> SendEmail(string toEmail, string toName, string subject, string htmlContent, string textContent = null)
        {
            if (string.IsNullOrEmpty(_settings.BrevoApiKey))
                return false;

            textContent ??= htmlContent; // 如果没传纯文本，就用 HTML

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
        /// 发送给管理员的邮件
        /// </summary>
        public async Task<bool> SendToAdmin(string subject, string htmlContent, string textContent = null)
        {
            return await SendEmail(_settings.AdminEmail, _settings.AdminName, subject, htmlContent, textContent);
        }
    }
}
