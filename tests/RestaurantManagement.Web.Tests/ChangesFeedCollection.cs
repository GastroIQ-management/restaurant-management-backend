using Xunit;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// Changes-feed tests that read twice run alone: the next cursor stops at the oldest open transaction on the
/// whole PostgreSQL server, so transactions from classes running in parallel make rows already read come back.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ChangesFeedCollection
{
    public const string Name = "Changes feed";
}
