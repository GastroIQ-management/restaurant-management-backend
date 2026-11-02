namespace RestaurantManagement.Shared.Services.ClientTime
{
    /// <summary>Request-scoped holder of the accepted times.</summary>
    public sealed class ClientTimeContext : IClientTimeContext
    {
        public ClientTimes? Times { get; private set; }

        public void Set(ClientTimes times) => Times = times;
    }
}
