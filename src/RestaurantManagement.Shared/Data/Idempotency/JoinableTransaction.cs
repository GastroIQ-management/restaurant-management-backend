using Microsoft.EntityFrameworkCore;

namespace RestaurantManagement.Shared.Data.Idempotency
{
    /// <summary>
    /// Begins a transaction on a context, or joins the one already open on that same context.
    /// An undo step registered on the handle runs once when the transaction ends without a commit.
    /// </summary>
    public sealed class JoinableTransaction : IAsyncDisposable
    {
        /// <summary>True when the transaction was already open, so commit and rollback belong to whoever opened it.</summary>
        public bool IsJoined => throw new NotImplementedException();

        public static Task<JoinableTransaction> BeginOrJoinAsync(DbContext context, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        /// <summary>Registers a step that undoes a change made outside this context, run once if the transaction does not commit.</summary>
        public void OnNotCommitted(Func<CancellationToken, Task> undo) => throw new NotImplementedException();

        public Task CommitAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task RollbackAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public ValueTask DisposeAsync() => throw new NotImplementedException();
    }
}
