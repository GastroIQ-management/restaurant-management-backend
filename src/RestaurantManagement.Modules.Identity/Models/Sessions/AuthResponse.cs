using System.Text.Json.Serialization;

namespace RestaurantManagement.Modules.Identity.Models.Sessions
{
    public class AuthResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Token { get; set; }
        public List<OrganizationChoice>? Organizations { get; set; }

        // Set only for native apps; web responses leave both out entirely.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RefreshToken { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? RefreshTokenExpiresAt { get; set; }
    }
}