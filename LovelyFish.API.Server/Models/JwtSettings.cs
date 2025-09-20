namespace LovelyFish.API.Server.Models
{
    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;
        public int ExpireMinutes { get; set; } = 60; // optional , Token expire time
    }
}
