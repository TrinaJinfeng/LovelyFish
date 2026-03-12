namespace LovelyFish.API.Server.Models
{
    // Configuration class for email and payment-related settings
    public class EmailSettings
    {
        //SMTP
        public string SmtpHost { get; set; } = "smtp.zoho.com";
        public int SmtpPort { get; set; } = 587; //TLS port
        public string SmtpUser { get; set; } // info@lovelyfishaquarium.co.nz
        public string SmtpPassword { get; set; } //zoho password


        // Default sender email and name
        public string FromEmail { get; set; } = "info@lovelyfishaquarium.co.nz";
        public string FromName { get; set; } = "LovelyFishAquarium";


        // Alternative sender info (optional, can be same as FromEmail/FromName)
        public string SenderEmail { get; set; } = "info@lovelyfishaquarium.co.nz";
        public string SenderName { get; set; } = "LovelyFishAquarium";


        // Frontend and backend URLs for generating links
        public string FrontendBaseUrl { get; set; } = "http://localhost:3000";
        public string ApiBaseUrl { get; set; } = "http://localhost:5062";

        // Admin email for receiving user messages (optional, defaults to FromEmail/FromName)
        public string AdminEmail { get; set; }
        public string AdminName { get; set; }

        // Bank account information for offline payment
        public string BankName { get; set; } = "Bank Name";
        public string AccountNumber { get; set; } = "***Bank Account Number***";
        public string AccountName { get; set; } = "Account Name";
    }
}
