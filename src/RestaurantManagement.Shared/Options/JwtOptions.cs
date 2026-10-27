namespace RestaurantManagement.Shared.Options
{
    public class JwtOptions
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public const string PlaceholderKey = "your-super-secret-key-at-least-32-characters-long-for-development";
        public int ExpireMinutes { get; set; } = 15;
        public int RefreshTokenExpireDays { get; set; } = 30;
        // Stub (T1 gives these their defaults and checks them at startup).
        public int SessionInactivityDays { get; set; }
        public int SessionAbsoluteDays { get; set; }
    }
}