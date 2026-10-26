namespace RestaurantManagement.Shared.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);

        /// <summary>Language is "en", "pt" or "fr"; any other value is answered in English.</summary>
        Task SendVerificationEmailAsync(string to, string firstName, string code, int expiryMinutes, string language);

        /// <summary>Language is "en", "pt" or "fr"; any other value is answered in English.</summary>
        Task SendPasswordResetEmailAsync(string to, string firstName, string code, int expiryMinutes, string language);
    }
}
