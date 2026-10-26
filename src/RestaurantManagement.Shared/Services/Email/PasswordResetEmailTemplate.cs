namespace RestaurantManagement.Shared.Services.Email
{
    public static class PasswordResetEmailTemplate
    {
        private static readonly LocalizedCodeEmail Email = new("password-reset-email", new()
        {
            ["en"] = "Reset your password",
            ["pt"] = "Redefina sua senha",
            ["fr"] = "Réinitialisez votre mot de passe"
        });

        public static string Subject(string language) => Email.Subject(language);

        public static string Build(string to, string firstName, string code, int expiryMinutes, string language)
            => Email.Build(to, firstName, code, expiryMinutes, language);
    }
}
