using NSubstitute;
using RestaurantManagement.Shared.Services.ClientTime;
using RestaurantManagement.Shared.Services.Organization;
using Shouldly;

namespace RestaurantManagement.Shared.Tests;

/// <summary>
/// Every saved record gets the accepted client times; a new location record also gets its business day
/// from the Organization contract. Cantina da Nonna buys from Hortifruti Batel; Ana works the cash shift at Batel.
/// </summary>
public class ClientTimeSaveChangesInterceptorTests
{
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid BatelId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000001");

    private static readonly ClientTimes Accepted = new(
        ActionTimeUtc: new DateTime(2026, 11, 4, 2, 40, 0, DateTimeKind.Utc),
        ActionOffsetMinutes: -180,
        CorrectedActionTime: new DateTime(2026, 11, 4, 2, 30, 0, DateTimeKind.Utc),
        ClockError: TimeSpan.FromMinutes(10),
        ReceivedAt: new DateTime(2026, 11, 4, 7, 30, 0, DateTimeKind.Utc));

    private static readonly DateOnly LocalDay = new(2026, 11, 3);

    private static TestPurchase Purchase() => new() { OrganizationId = CantinaId, Supplier = "Hortifruti Batel" };

    private static TestCashShift CashShift() => new() { OrganizationId = CantinaId, LocationId = BatelId, Cashier = "Ana" };

    private static ILocationBusinessDay BusinessDayReturning(DateOnly? day)
    {
        var contract = Substitute.For<ILocationBusinessDay>();
        contract.GetBusinessDayAsync(Arg.Any<Guid>(), Arg.Any<DateTime>()).Returns(day);
        return contract;
    }

    // Outcomes 1 and 2
    [Fact]
    [Trait("Rule", "SH116")]
    public async Task SaveChangesAsync_NewRowWithAcceptedTimes_StampsAllFiveTimes()
    {
        // Arrange
        using var database = new ClientTimeTestDatabase();
        using var context = database.CreateContext(ClientTimeTestDatabase.CreateInterceptor(Accepted));
        var purchase = Purchase();
        context.Purchases.Add(purchase);

        // Act
        await context.SaveChangesAsync();

        // Assert
        purchase.ClientActionTime.ShouldBe(Accepted.ActionTimeUtc, "the device action time is stored");
        purchase.ClientActionOffsetMinutes.ShouldBe(-180, "the device offset is stored");
        purchase.CorrectedActionTime.ShouldBe(Accepted.CorrectedActionTime, "the corrected action time is stored");
        purchase.ClockError.ShouldBe(Accepted.ClockError, "the clock error is stored");
        purchase.ReceivedAt.ShouldBe(Accepted.ReceivedAt, "the server receive time is stored");
    }

    // Outcome 1: updates refresh the times like UpdatedAt
    [Fact]
    [Trait("Rule", "SH117")]
    public async Task SaveChangesAsync_ChangedRowWithAcceptedTimes_RefreshesTheTimes()
    {
        // Arrange
        using var database = new ClientTimeTestDatabase();
        var older = new ClientTimes(Accepted.ActionTimeUtc.AddDays(-1), 0, Accepted.CorrectedActionTime.AddDays(-1), TimeSpan.Zero, Accepted.ReceivedAt.AddDays(-1));
        using var context = database.CreateContext(ClientTimeTestDatabase.CreateInterceptor(older));
        var purchase = Purchase();
        context.Purchases.Add(purchase);
        await context.SaveChangesAsync();
        using var updateContext = database.CreateContext(ClientTimeTestDatabase.CreateInterceptor(Accepted));
        var stored = updateContext.Purchases.Single();
        stored.Supplier = "Hortifruti Centro";

        // Act
        await updateContext.SaveChangesAsync();

        // Assert
        stored.CorrectedActionTime.ShouldBe(Accepted.CorrectedActionTime, "an edit stamps the new action time");
        stored.ReceivedAt.ShouldBe(Accepted.ReceivedAt, "an edit stamps the new receive time");
        stored.ClockError.ShouldBe(Accepted.ClockError, "an edit stamps the new clock error");
    }

    // Edge: no signed-in write
    [Fact]
    [Trait("Rule", "SH118")]
    public async Task SaveChangesAsync_NoAcceptedTimes_StampsNothing()
    {
        // Arrange
        using var database = new ClientTimeTestDatabase();
        using var context = database.CreateContext(ClientTimeTestDatabase.CreateInterceptor(null));
        var purchase = Purchase();
        context.Purchases.Add(purchase);

        // Act
        await context.SaveChangesAsync();

        // Assert
        purchase.CorrectedActionTime.ShouldBeNull("a save outside a signed-in write keeps the time columns empty");
        purchase.ReceivedAt.ShouldBeNull("a save outside a signed-in write keeps the time columns empty");
        purchase.ClientActionTime.ShouldBeNull("a save outside a signed-in write keeps the time columns empty");
    }

    // Outcome 3
    [Fact]
    [Trait("Rule", "SH119")]
    public async Task SaveChangesAsync_NewLocationRow_GetsTheBusinessDayOfTheCorrectedTime()
    {
        // Arrange
        using var database = new ClientTimeTestDatabase();
        var contract = BusinessDayReturning(LocalDay);
        using var context = database.CreateContext(ClientTimeTestDatabase.CreateInterceptor(Accepted, contract));
        var shift = CashShift();
        context.CashShifts.Add(shift);

        // Act
        await context.SaveChangesAsync();

        // Assert
        shift.BusinessDay.ShouldBe(LocalDay, "a new location record stores the business day the Organization contract answers");
        await contract.Received(1).GetBusinessDayAsync(BatelId, Accepted.CorrectedActionTime);
    }

    // Outcome 3: edge
    [Fact]
    [Trait("Rule", "SH120")]
    public async Task SaveChangesAsync_EditedLocationRow_KeepsItsBusinessDay()
    {
        // Arrange
        using var database = new ClientTimeTestDatabase();
        using var createContext = database.CreateContext(ClientTimeTestDatabase.CreateInterceptor(Accepted, BusinessDayReturning(LocalDay)));
        createContext.CashShifts.Add(CashShift());
        await createContext.SaveChangesAsync();
        var otherContract = BusinessDayReturning(new DateOnly(2026, 11, 9));
        using var editContext = database.CreateContext(ClientTimeTestDatabase.CreateInterceptor(Accepted, otherContract));
        var stored = editContext.CashShifts.Single();
        stored.Cashier = "Bruno";

        // Act
        await editContext.SaveChangesAsync();

        // Assert
        stored.BusinessDay.ShouldBe(LocalDay, "an edit never moves a record to another business day");
        await otherContract.DidNotReceive().GetBusinessDayAsync(Arg.Any<Guid>(), Arg.Any<DateTime>());
    }

    // Edge: unknown location
    [Fact]
    [Trait("Rule", "SH121")]
    public async Task SaveChangesAsync_ContractKnowsNoSuchLocation_LeavesTheBusinessDayEmpty()
    {
        // Arrange
        using var database = new ClientTimeTestDatabase();
        using var context = database.CreateContext(ClientTimeTestDatabase.CreateInterceptor(Accepted, BusinessDayReturning(null)));
        var shift = CashShift();
        context.CashShifts.Add(shift);

        // Act
        await context.SaveChangesAsync();

        // Assert
        shift.BusinessDay.ShouldBeNull("no business day is invented for a location the contract does not know");
        shift.CorrectedActionTime.ShouldBe(Accepted.CorrectedActionTime, "the times are still stamped");
    }

    // Outcome 3: records with no location store no business day, and the contract is not needed
    [Fact]
    [Trait("Rule", "SH122")]
    public async Task SaveChangesAsync_RowNotTiedToALocationAndNoContractRegistered_SavesWithoutResolvingTheContract()
    {
        // Arrange
        using var database = new ClientTimeTestDatabase();
        using var context = database.CreateContext(ClientTimeTestDatabase.CreateInterceptor(Accepted));
        var purchase = Purchase();
        context.Purchases.Add(purchase);

        // Act
        var saved = await context.SaveChangesAsync();

        // Assert
        saved.ShouldBe(1, "the contract is resolved only when a location record is saved, so a context that Organization itself uses is not circular");
        purchase.CorrectedActionTime.ShouldBe(Accepted.CorrectedActionTime);
    }

    // Failure: the synchronous save cannot await the contract
    [Fact]
    [Trait("Rule", "SH123")]
    public void SaveChanges_NewLocationRowOnASynchronousSave_FailsInsteadOfSkippingTheBusinessDay()
    {
        // Arrange
        using var database = new ClientTimeTestDatabase();
        using var context = database.CreateContext(ClientTimeTestDatabase.CreateInterceptor(Accepted, BusinessDayReturning(LocalDay)));
        context.CashShifts.Add(CashShift());

        // Act / Assert
        Should.Throw<InvalidOperationException>(() => context.SaveChanges(),
            "a location record saved synchronously must fail loudly, never store a missing business day");
    }
}
