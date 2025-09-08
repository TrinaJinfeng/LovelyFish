using LovelyFish.API.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly EmailSettings _emailSettings;

    // Constructor: inject IOptions<EmailSettings> to access configuration
    public SettingsController(IOptions<EmailSettings> options)
    {
        _emailSettings = options.Value; // Get the EmailSettings values from configuration
    }

    // ==================== Get Email/Bank Account Settings ====================
    [HttpGet("email")]
    public IActionResult GetEmailSettings()
    {
        // Return only necessary info to frontend (Bank info)
        return Ok(new
        {
            BankName = _emailSettings.BankName,         // Bank name
            AccountName = _emailSettings.AccountName,   // Account holder name
            AccountNumber = _emailSettings.AccountNumber, // Bank account number
        });
    }
}
