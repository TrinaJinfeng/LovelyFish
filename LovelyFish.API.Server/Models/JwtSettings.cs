namespace LovelyFish.API.Server.Models
{
    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;   // Secret key
        public string Issuer { get; set; } = string.Empty;   // Token issuer
        public string Audience { get; set; } = string.Empty; // Token audience
        public int ExpiryMinutes { get; set; } = 60;         // default 60mins
    }
}
