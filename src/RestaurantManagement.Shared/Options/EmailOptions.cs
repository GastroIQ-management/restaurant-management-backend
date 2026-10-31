namespace RestaurantManagement.Shared.Options
{
    public class EmailOptions
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUser { get; set; } = string.Empty;
        public string SmtpPass { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "RestaurantManagement";
        // Public address of the logo shown in emails; when empty the email shows the text "GastroIQ".
        public string LogoUrl { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }
}
