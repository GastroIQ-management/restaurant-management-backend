namespace RestaurantManagement.Shared.Services.Email
{
    public static class VerificationEmailTemplate
    {
        private static readonly LocalizedCodeEmail Email = new("verification-email", new()
        {
            ["en"] = "Verify your email",
            ["pt"] = "Confirme seu e-mail",
            ["fr"] = "Confirmez votre adresse e-mail"
        });

        public static string Subject(string language) => Email.Subject(language);

        public static string Build(string to, string firstName, string code, int expiryMinutes, string language)
            => Email.Build(to, firstName, code, expiryMinutes, language);
    }
}
