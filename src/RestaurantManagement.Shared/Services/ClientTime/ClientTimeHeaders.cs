namespace RestaurantManagement.Shared.Services.ClientTime
{
    /// <summary>Names of the two headers every signed-in write carries.</summary>
    public static class ClientTimeHeaders
    {
        /// <summary>When the user acted on the device (RFC 3339 date and time with an offset).</summary>
        public const string ActionTime = "X-Client-Action-Time";

        /// <summary>The device clock at send time (RFC 3339 date and time with an offset).</summary>
        public const string SentAt = "X-Client-Sent-At";
    }
}
