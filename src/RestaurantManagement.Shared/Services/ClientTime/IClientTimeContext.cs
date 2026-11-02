namespace RestaurantManagement.Shared.Services.ClientTime
{
    /// <summary>The accepted times of the current signed-in write. Empty outside a signed-in write.</summary>
    public interface IClientTimeContext
    {
        ClientTimes? Times { get; }

        void Set(ClientTimes times);
    }
}
