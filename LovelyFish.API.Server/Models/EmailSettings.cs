namespace LovelyFish.API.Server.Models
{
    public class EmailSettings
    {
        // Brevo API Key
        public string BrevoApiKey { get; set; }

        // 默认发件人信息
        public string FromEmail { get; set; } = "lovelyfishaquarium@outlook.com";
        public string FromName { get; set; } = "LovelyFishAquarium";

        public string SenderEmail { get; set; } = "lovelyfishaquarium@outlook.com";
        public string SenderName { get; set; } = "LovelyFishAquarium";

        public string FrontendBaseUrl { get; set; } = "http://localhost:3000";


        // 管理员接收邮箱信息（可选，不配置则使用 FromEmail/FromName）
        public string AdminEmail { get; set; }
        public string AdminName { get; set; }

        // 银行信息
        public string BankName { get; set; } = "Bank Name";
        public string AccountNumber { get; set; } = "***Bank Account Number***";
        public string AccountName { get; set; } = "Account Name";

    }
}
