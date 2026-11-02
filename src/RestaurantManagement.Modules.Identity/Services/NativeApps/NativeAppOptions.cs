namespace RestaurantManagement.Modules.Identity.Services.NativeApps
{
    /// <summary>Settings of the native apps (section "NativeApp").</summary>
    public class NativeAppOptions
    {
        public const string SectionName = "NativeApp";

        /// <summary>Origins that count as native apps; an empty list turns native mode off.</summary>
        public List<string> Origins { get; set; } = new();
    }
}
