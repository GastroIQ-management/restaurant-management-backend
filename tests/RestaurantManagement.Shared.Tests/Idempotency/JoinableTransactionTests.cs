using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Shared.Data.Idempotency;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Idempotency;

/// <summary>A service can begin a transaction, or join the one the write filter already opened on the same context.</summary>
public class JoinableTransactionTests : IDisposable
{
    private static readonly Guid NoteId = Guid.Parse("77777777-0000-4000-8000-0000000000e1");

    private readonly IdempotencyTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task AddNoteAsync(IdempotencyTestDbContext context)
    {
        context.Notes.Add(new TestKitchenNote { Id = NoteId, Text = "Mesa 12: sem glúten" });
        await context.SaveChangesAsync();
    }

    private async Task<int> CountNotesAsync()
    {
        using var check = _database.CreateContext();
        return await check.Notes.CountAsync();
    }

    [Fact]
    [Trait("Rule", "SH93")]
    public async Task BeginOrJoinAsync_NoOpenTransaction_OwnsItAndCommitSavesTheWork()
    {
        // Arrange
        using var context = _database.CreateContext();

        // Act
        await using var transaction = await JoinableTransaction.BeginOrJoinAsync(context);
        await AddNoteAsync(context);
        await transaction.CommitAsync();

        // Assert
        transaction.IsJoined.ShouldBeFalse("with no open transaction the helper opens its own");
        (await CountNotesAsync()).ShouldBe(1, "committing the owned transaction saves the work");
    }

    [Fact]
    [Trait("Rule", "SH94")]
    public async Task CommitAsync_JoinedTransaction_SavesNothingUntilTheOpenerCommits()
    {
        // Arrange
        using var context = _database.CreateContext();
        await using var opener = await JoinableTransaction.BeginOrJoinAsync(context);
        await using var joined = await JoinableTransaction.BeginOrJoinAsync(context);
        await AddNoteAsync(context);

        // Act
        await joined.CommitAsync();

        // Assert
        joined.IsJoined.ShouldBeTrue("a transaction already open on the same context is joined, not nested");
        context.Database.CurrentTransaction.ShouldNotBeNull("a joined commit leaves the transaction open for its opener");
        await opener.CommitAsync();
        (await CountNotesAsync()).ShouldBe(1, "the work is saved when the opener commits");
    }

    [Fact]
    [Trait("Rule", "SH95")]
    public async Task RollbackAsync_OpenerRollsBackAfterAJoinedCommit_DiscardsTheJoinedWork()
    {
        // Arrange
        using var context = _database.CreateContext();
        await using var opener = await JoinableTransaction.BeginOrJoinAsync(context);
        await using var joined = await JoinableTransaction.BeginOrJoinAsync(context);
        await AddNoteAsync(context);
        await joined.CommitAsync();

        // Act
        await opener.RollbackAsync();

        // Assert
        (await CountNotesAsync()).ShouldBe(0, "the joined work is discarded with the opener's transaction");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Rule", "SH96")]
    public async Task UndoStep_TransactionEndsWithoutACommit_RunsExactlyOnce(bool commitFails)
    {
        // Arrange
        using var context = commitFails ? _database.CreateContext(new FailingCommitInterceptor()) : _database.CreateContext();
        var undone = 0;
        await using var transaction = await JoinableTransaction.BeginOrJoinAsync(context);
        transaction.OnNotCommitted(_ =>
        {
            undone++;
            return Task.CompletedTask;
        });
        await AddNoteAsync(context);

        // Act
        if (commitFails)
            await Should.ThrowAsync<InvalidOperationException>(() => transaction.CommitAsync());
        else
            await transaction.RollbackAsync();
        await transaction.DisposeAsync();

        // Assert
        undone.ShouldBe(1, "a change made in another module is undone once when the transaction ends without a commit");
        (await CountNotesAsync()).ShouldBe(0, "nothing is saved");
    }

    [Fact]
    [Trait("Rule", "SH97")]
    public async Task UndoStep_TransactionCommits_NeverRuns()
    {
        // Arrange
        using var context = _database.CreateContext();
        var undone = 0;
        await using var transaction = await JoinableTransaction.BeginOrJoinAsync(context);
        transaction.OnNotCommitted(_ =>
        {
            undone++;
            return Task.CompletedTask;
        });
        await AddNoteAsync(context);

        // Act
        await transaction.CommitAsync();
        await transaction.DisposeAsync();

        // Assert
        undone.ShouldBe(0, "a committed transaction keeps the change made in another module");
    }

    [Fact]
    [Trait("Rule", "SH98")]
    public async Task UndoStep_RegisteredByAJoinedHandle_RunsOnceWhenTheOpenerRollsBack()
    {
        // Arrange
        using var context = _database.CreateContext();
        var undone = 0;
        await using var opener = await JoinableTransaction.BeginOrJoinAsync(context);
        await using var joined = await JoinableTransaction.BeginOrJoinAsync(context);
        joined.OnNotCommitted(_ =>
        {
            undone++;
            return Task.CompletedTask;
        });
        await AddNoteAsync(context);
        await joined.CommitAsync();

        // Act
        await opener.RollbackAsync();
        await opener.DisposeAsync();

        // Assert
        undone.ShouldBe(1, "the undo step runs whoever opened the transaction, exactly once");
    }
}
