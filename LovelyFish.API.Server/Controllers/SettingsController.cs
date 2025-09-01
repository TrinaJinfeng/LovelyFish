using LovelyFish.API.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly EmailSettings _emailSettings;

    public SettingsController(IOptions<EmailSettings> options)
    {
        _emailSettings = options.Value;
    }

    [HttpGet("email")]
    public IActionResult GetEmailSettings()
    {
        return Ok(new
        {
            BankName = _emailSettings.BankName,
            AccountName = _emailSettings.AccountName,
            AccountNumber = _emailSettings.AccountNumber,
        });
    }
}
