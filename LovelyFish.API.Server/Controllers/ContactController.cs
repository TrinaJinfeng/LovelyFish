using Microsoft.AspNetCore.Mvc;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Options;
using LovelyFish.API.Server.Models;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly string _sendGridKey;

    public ContactController(IOptions<EmailSettings> emailSettings)
    {
        _sendGridKey = emailSettings.Value.SendGridApiKey;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ContactMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Name) ||
            string.IsNullOrWhiteSpace(message.Email) ||
            string.IsNullOrWhiteSpace(message.Message))
        {
            return BadRequest(new { message = "请填写所有字段。" });
        }

        if (string.IsNullOrEmpty(_sendGridKey))
        {
            return StatusCode(500, new { message = "邮件未发送，请联系管理员。" });
        }

        try
        {
            var client = new SendGridClient(_sendGridKey);

            var from = new EmailAddress("jinfengtrina@gmail.com", "LovelyFishAquarium");
            var subject = "新用户消息";
            var to = new EmailAddress("jinfengtrina@gmail.com", "管理员");

            var plainTextContent = $"姓名: {message.Name}\n邮箱: {message.Email}\n消息: {message.Message}";
            var htmlContent = $"<p><strong>姓名:</strong> {message.Name}</p>" +
                              $"<p><strong>邮箱:</strong> {message.Email}</p>" +
                              $"<p><strong>消息:</strong> {message.Message}</p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
                return Ok(new { message = "消息已发送！" });

            return StatusCode(500, new { message = "邮件发送失败，请联系管理员。" });
        }
        catch
        {
            return StatusCode(500, new { message = "邮件发送失败，请联系管理员。" });
        }
    }
}

public class ContactMessage
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Message { get; set; }
}
