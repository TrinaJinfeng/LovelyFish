namespace LovelyFish.API.Server.Models
{
    // Configuration class for email and payment-related settings
    public class EmailSettings
    {
        // Brevo (formerly Sendinblue) API Key for sending emails
        public string BrevoApiKey { get; set; }

        // Default sender email and name
        public string FromEmail { get; set; } = "lovelyfishaquarium@outlook.com";
        public string FromName { get; set; } = "LovelyFishAquarium";

        // Alternative sender info (optional, can be same as FromEmail/FromName)
        public string SenderEmail { get; set; } = "lovelyfishaquarium@outlook.com";
        public string SenderName { get; set; } = "LovelyFishAquarium";

        // Frontend and backend URLs for generating links
        public string FrontendBaseUrl { get; set; } = "https://kind-coast-0e9e19400.1.azurestaticapps.net";
        public string ApiBaseUrl { get; set; } = "https://lovelyfish-backend-esgtdkf7h0e2ambg.australiaeast-01.azurewebsites.net";

        // Admin email for receiving user messages (optional, defaults to FromEmail/FromName)
        public string AdminEmail { get; set; }
        public string AdminName { get; set; }

        // Bank account information for offline payment
        public string BankName { get; set; } = "Bank Name";
        public string AccountNumber { get; set; } = "***Bank Account Number***";
        public string AccountName { get; set; } = "Account Name";
    }
}
