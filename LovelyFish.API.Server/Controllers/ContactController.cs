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

    // Inject EmailService and EmailSettings
    public ContactController(EmailService emailService, IOptions<EmailSettings> options)
    {
        _emailService = emailService;
        _settings = options.Value;
    }

    // POST api/contact
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ContactMessage message)
    {
        // Basic validation: check if all fields are filled
        if (string.IsNullOrWhiteSpace(message.Name) ||
            string.IsNullOrWhiteSpace(message.Email) ||
            string.IsNullOrWhiteSpace(message.Message))
        {
            return BadRequest(new { message = "Please fill in all fields." });
        }

        // Send email using EmailService; recipient info comes from configuration
        var success = await _emailService.SendEmail(
            _settings.AdminEmail ?? _settings.FromEmail,   // Use AdminEmail if configured, else FromEmail
            _settings.AdminName ?? _settings.FromName,
            "New Contact Message",
            $"<p><strong>Name:</strong> {message.Name}</p>" +
            $"<p><strong>Email:</strong> {message.Email}</p>" +
            $"<p><strong>Message:</strong> {message.Message}</p>"
        );

        // Return response based on success/failure
        return success
            ? Ok(new { message = "Message sent successfully!" })
            : StatusCode(500, new { message = "Failed to send email. Please contact administrator." });
    }
}

// DTO for receiving contact message from frontend
public class ContactMessage
{
    public string Name { get; set; }     // User's name
    public string Email { get; set; }    // User's email
    public string Message { get; set; }  // Message content
}
