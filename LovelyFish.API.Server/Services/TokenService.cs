using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LovelyFish.API.Server.Models;
using LovelyFish.API.Server.Services;
using Microsoft.AspNetCore.Identity;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    // Inject JwtSettings from configuration
    public TokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>
    /// Generate a signed JWT token for a given user.
    /// </summary>
    /// <param name="user">The authenticated application user.</param>
    /// <param name="userManager">ASP.NET Core Identity user manager for role retrieval.</param>
    /// <returns>JWT token string.</returns>
    public async Task<string> GenerateToken(ApplicationUser user, UserManager<ApplicationUser> userManager)
    {
        // Get all roles assigned to the user
        var userRoles = await userManager.GetRolesAsync(user);

        // Standard claims included in JWT
        var claims = new List<Claim>
        {
            // "sub" (subject) represents the user ID
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

            // "unique_name" represents the username or email
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Email ?? ""),

            // "jti" is a unique token identifier (used for token replay protection)
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role claims so that [Authorize(Roles = "...")] works
        claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Create signing key using the configured secret
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));

        // Define the signing algorithm (HMAC-SHA256)
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Create the token with issuer, audience, claims, expiry, and signing credentials
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: creds
        );

        // Serialize the token to a compact JWT string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}