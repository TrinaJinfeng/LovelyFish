using Microsoft.AspNetCore.Mvc;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    // 请替换成你在 SendGrid 后台生成的 API Key
    private readonly string SendGridApiKey = "***REMOVED***";

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ContactMessage message)
    {
        try
        {
            var client = new SendGridClient(SendGridApiKey);

            var from = new EmailAddress("jinfengtrina@gmail.com", "LovelyFishAquarium");
            var subject = "新用户消息";
            var to = new EmailAddress("jinfengtrina@gmail.com", "管理员"); // 接收邮箱
            var plainTextContent = $"姓名: {message.Name}\n邮箱: {message.Email}\n消息: {message.Message}";
            var htmlContent = $"<p><strong>姓名:</strong> {message.Name}</p>" +
                              $"<p><strong>邮箱:</strong> {message.Email}</p>" +
                              $"<p><strong>消息:</strong> {message.Message}</p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
            {
                return Ok(new { message = "消息已发送！" });
            }
            else
            {
                var body = await response.Body.ReadAsStringAsync();
                return StatusCode(500, new { message = "发送失败", error = body });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "发送失败", error = ex.Message });
        }
    }
}

// 请求数据模型
public class ContactMessage
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Message { get; set; }
}
