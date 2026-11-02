using Microsoft.Extensions.Configuration;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The native app origins the app ships with, read from the Web project's real appsettings.json.
/// Other tests set their own values, so without this a change to the shipped list would fail nothing.
/// </summary>
public class ShippedNativeOriginsTests
{
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "RestaurantManagement.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("RestaurantManagement.sln not found above the test output folder.");
    }

    [Fact]
    [Trait("Rule", "T172")]
    public void Configuration_ShippedNativeAppOrigins_AreExactlyTheTwoNativeOrigins()
    {
        // Arrange
        var shipped = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(RepositoryRoot(), "src", "RestaurantManagement.Web", "appsettings.json"), optional: false)
            .Build();

        // Act
        var origins = shipped.GetSection("NativeApp:Origins").Get<string[]>();

        // Assert
        origins.ShouldNotBeNull("the shipped file sets the native origins itself");
        origins.ShouldBe(new[] { "https://localhost", "capacitor://localhost" }, ignoreOrder: true, "only the Android and iOS app origins count as native");
    }
}
