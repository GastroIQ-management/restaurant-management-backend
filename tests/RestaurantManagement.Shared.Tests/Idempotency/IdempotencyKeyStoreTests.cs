using System.Text;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Shared.Data.Idempotency;
using RestaurantManagement.Shared.Services.Idempotency;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Idempotency;

/// <summary>The key store decides, for a user, an organization and a key, whether a write is new, running, refused or a replay.</summary>
public class IdempotencyKeyStoreTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid BrunoId = Guid.Parse("33333333-cccc-4ccc-8ccc-00000000000c");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid TrattoriaId = Guid.Parse("b2b2b2b2-0000-4000-8000-000000000002");
    private static readonly Guid Key = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e11");

    private readonly IdempotencyTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static StoredResponse MenuCreatedAnswer()
        => new(201, "application/json", Encoding.UTF8.GetBytes("""{"id":"55555555-0000-4000-8000-000000000001","name":"Jantar"}"""), "/api/v1/menus/55555555-0000-4000-8000-000000000001");

    // Failure first
    [Theory]
    [InlineData(false, "hash-of-the-same-request", IdempotencyOutcome.InProgress)]
    [InlineData(false, "hash-of-another-request", IdempotencyOutcome.Reused)]
    [InlineData(true, "hash-of-another-request", IdempotencyOutcome.Reused)]
    [Trait("Rule", "SH80")]
    public async Task ReserveAsync_KeyAlreadyStored_RefusesByStateAndRequest(bool firstFinished, string secondHash, IdempotencyOutcome expected)
    {
        // Arrange
        var first = _database.CreateStore(Now);
        await first.ReserveAsync(AnaId, CantinaId, Key, "hash-of-the-same-request");
        if (firstFinished)
            await first.CompleteAsync(AnaId, CantinaId, Key, MenuCreatedAnswer());

        // Act
        var second = await _database.CreateStore(Now).ReserveAsync(AnaId, CantinaId, Key, secondHash);

        // Assert
        second.Outcome.ShouldBe(expected, "a key still running answers in progress, and a key sent with another request is reused, whether the first one finished or not");
        second.Response.ShouldBeNull("a refused key never carries a saved answer");
    }

    [Theory]
    [InlineData("otherUser")]
    [InlineData("otherOrganization")]
    [InlineData("noOrganization")]
    [Trait("Rule", "SH81")]
    public async Task ReserveAsync_SameKeyFromAnotherUserOrOrganization_IsNewAndNeverReturnsTheFirstAnswer(string scope)
    {
        // Arrange
        var first = _database.CreateStore(Now);
        await first.ReserveAsync(AnaId, CantinaId, Key, "hash-of-the-same-request");
        await first.CompleteAsync(AnaId, CantinaId, Key, MenuCreatedAnswer());
        var (userId, organizationId) = scope switch
        {
            "otherUser" => (BrunoId, (Guid?)CantinaId),
            "otherOrganization" => (AnaId, (Guid?)TrattoriaId),
            _ => (AnaId, (Guid?)null)
        };

        // Act
        var second = await _database.CreateStore(Now).ReserveAsync(userId, organizationId, Key, "hash-of-the-same-request");

        // Assert
        second.Outcome.ShouldBe(IdempotencyOutcome.Reserved, "a key is matched only for the same user inside the same organization");
        second.Response.ShouldBeNull("another user or organization never receives the first answer");
        using var check = _database.CreateContext();
        var rows = await check.Set<IdempotencyKey>().ToListAsync();
        rows.Count.ShouldBe(2, "each user and organization has its own row");
        rows.Single(r => r.UserId == AnaId && r.OrganizationId == CantinaId).State.ShouldBe(IdempotencyKeyState.Completed, "the first user's saved answer is untouched");
    }

    // Edge: the clock decides when a row stops counting
    [Theory]
    [InlineData(false, 8 * 24 * 3600 + 1, IdempotencyOutcome.Reserved)]
    [InlineData(false, 7 * 24 * 3600 + 23 * 3600, IdempotencyOutcome.Replay)]
    [InlineData(true, 121, IdempotencyOutcome.Reserved)]
    [InlineData(true, 119, IdempotencyOutcome.InProgress)]
    [Trait("Rule", "SH82")]
    public async Task ReserveAsync_KeyOlderThanRetentionOrAbandoned_IsTakenOver(bool leftInProgress, int ageInSeconds, IdempotencyOutcome expected)
    {
        // Arrange
        var first = _database.CreateStore(Now);
        await first.ReserveAsync(AnaId, CantinaId, Key, "hash-of-the-same-request");
        if (!leftInProgress)
            await first.CompleteAsync(AnaId, CantinaId, Key, MenuCreatedAnswer());

        // Act
        var second = await _database.CreateStore(Now.AddSeconds(ageInSeconds)).ReserveAsync(AnaId, CantinaId, Key, "hash-of-the-same-request");

        // Assert
        second.Outcome.ShouldBe(expected, "a key past the 8-day retention, or in progress for over 120 seconds, counts as new; before that it still counts");
    }

    [Fact]
    [Trait("Rule", "SH83")]
    public async Task PurgeExpiredAsync_OldAndFreshKeys_RemovesOnlyTheExpiredOnes()
    {
        // Arrange
        var old = _database.CreateStore(Now.AddDays(-9));
        await old.ReserveAsync(AnaId, CantinaId, Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e21"), "hash-old");
        await old.CompleteAsync(AnaId, CantinaId, Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e21"), MenuCreatedAnswer());
        var recent = _database.CreateStore(Now.AddDays(-7));
        await recent.ReserveAsync(AnaId, CantinaId, Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e22"), "hash-recent");
        await _database.CreateStore(Now).ReserveAsync(AnaId, CantinaId, Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e23"), "hash-today");

        // Act
        var deleted = await _database.CreateStore(Now).PurgeExpiredAsync();

        // Assert
        deleted.ShouldBe(1, "only the key older than 8 days is purged");
        using var check = _database.CreateContext();
        (await check.Set<IdempotencyKey>().Select(k => k.RequestHash).ToListAsync())
            .ShouldBe(new[] { "hash-recent", "hash-today" }, ignoreOrder: true, customMessage: "keys inside the retention time are kept");
    }

    [Fact]
    [Trait("Rule", "SH84")]
    public async Task ReserveAsync_NewKey_SavesItInProgressAtOnceWithOnlyTheRequestHash()
    {
        // Arrange
        var store = _database.CreateStore(Now);

        // Act
        var reservation = await store.ReserveAsync(AnaId, null, Key, "hash-of-the-same-request");

        // Assert
        reservation.Outcome.ShouldBe(IdempotencyOutcome.Reserved, "a key never seen runs normally");
        using var check = _database.CreateContext();
        var row = await check.Set<IdempotencyKey>().SingleAsync();
        row.State.ShouldBe(IdempotencyKeyState.InProgress, "the key is saved at once as running, so a second request is refused");
        row.RequestHash.ShouldBe("hash-of-the-same-request", "the request is kept only as a hash");
        row.ResponseBody.ShouldBeNull("no answer is saved before the write succeeds");
        row.OrganizationId.ShouldBeNull("a user with no organization in the token is matched with no organization");
        row.CreatedAt.ShouldBe(Now.UtcDateTime, "the row is dated by the clock");
    }

    [Fact]
    [Trait("Rule", "SH85")]
    public async Task ReserveAsync_KeyCompletedWithTheSameRequest_ReplaysTheSavedAnswer()
    {
        // Arrange
        var first = _database.CreateStore(Now);
        await first.ReserveAsync(AnaId, CantinaId, Key, "hash-of-the-same-request");
        await first.CompleteAsync(AnaId, CantinaId, Key, MenuCreatedAnswer());

        // Act
        var second = await _database.CreateStore(Now.AddMinutes(5)).ReserveAsync(AnaId, CantinaId, Key, "hash-of-the-same-request");

        // Assert
        second.Outcome.ShouldBe(IdempotencyOutcome.Replay, "the same request with the same key is answered from the saved answer");
        var saved = second.Response.ShouldNotBeNull();
        saved.Status.ShouldBe(201, "the replay keeps the status");
        saved.ContentType.ShouldBe("application/json", "the replay keeps the content type");
        Encoding.UTF8.GetString(saved.Body).ShouldContain("Jantar", Case.Sensitive, "the replay keeps the body");
        saved.Location.ShouldBe("/api/v1/menus/55555555-0000-4000-8000-000000000001", "the replay keeps the Location header");
    }
}
