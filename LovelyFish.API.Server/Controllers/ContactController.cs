using Microsoft.AspNetCore.Mvc;
using LovelyFish.API.Server.Models;
using LovelyFish.API.Server.Services;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly EmailService _emailService;
    private readonly EmailSettings _settings;

    // 注入 EmailService 和 EmailSettings
    public ContactController(EmailService emailService, IOptions<EmailSettings> options)
    {
        _emailService = emailService;
        _settings = options.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ContactMessage message)
    {
        // 基础校验
        if (string.IsNullOrWhiteSpace(message.Name) ||
            string.IsNullOrWhiteSpace(message.Email) ||
            string.IsNullOrWhiteSpace(message.Message))
        {
            return BadRequest(new { message = "请填写所有字段。" });
        }

        // 调用 EmailService 发送邮件，收件人信息从配置读取
        var success = await _emailService.SendEmail(
            _settings.AdminEmail ?? _settings.FromEmail,   // 如果 AdminEmail 没配置，就用 FromEmail
            _settings.AdminName ?? _settings.FromName,
            "新用户消息",
            $"<p><strong>姓名:</strong> {message.Name}</p>" +
            $"<p><strong>邮箱:</strong> {message.Email}</p>" +
            $"<p><strong>消息:</strong> {message.Message}</p>"
        );

        return success
            ? Ok(new { message = "消息已发送！" })
            : StatusCode(500, new { message = "邮件发送失败，请联系管理员。" });
    }
}

// 接收前端传来的消息数据
public class ContactMessage
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Message { get; set; }
}
