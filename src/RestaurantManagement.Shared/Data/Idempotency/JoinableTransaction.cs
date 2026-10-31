using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace RestaurantManagement.Shared.Data.Idempotency
{
    /// <summary>
    /// Begins a transaction on a context, or joins the one already open on that same context.
    /// An undo step registered on the handle runs once when the transaction ends without a commit.
    /// </summary>
    public sealed class JoinableTransaction : IAsyncDisposable
    {
        // The undo steps belong to the transaction, not to a handle, so owner and joined handles share them.
        private static readonly ConditionalWeakTable<IDbContextTransaction, UndoSteps> StepsByTransaction = new();

        private readonly IDbContextTransaction _transaction;
        private readonly UndoSteps _steps;
        private bool _ended;

        private JoinableTransaction(IDbContextTransaction transaction, bool isJoined)
        {
            _transaction = transaction;
            IsJoined = isJoined;
            _steps = StepsByTransaction.GetOrCreateValue(transaction);
        }

        /// <summary>True when the transaction was already open, so commit and rollback belong to whoever opened it.</summary>
        public bool IsJoined { get; }

        public static async Task<JoinableTransaction> BeginOrJoinAsync(DbContext context, CancellationToken cancellationToken = default)
        {
            var open = context.Database.CurrentTransaction;
            return open is not null
                ? new JoinableTransaction(open, isJoined: true)
                : new JoinableTransaction(await context.Database.BeginTransactionAsync(cancellationToken), isJoined: false);
        }

        /// <summary>Registers a step that undoes a change made outside this context, run once if the transaction does not commit.</summary>
        public void OnNotCommitted(Func<CancellationToken, Task> undo) => _steps.Add(undo);

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (IsJoined || _ended)
                return;

            try
            {
                await _transaction.CommitAsync(cancellationToken);
                _ended = true;
                _steps.Discard();
            }
            catch
            {
                await EndWithoutCommitAsync(cancellationToken);
                throw;
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (IsJoined || _ended)
                return;

            await EndWithoutCommitAsync(cancellationToken, reportRollbackFailure: true);
        }

        public async ValueTask DisposeAsync()
        {
            if (IsJoined)
                return;

            if (!_ended)
                await EndWithoutCommitAsync(CancellationToken.None);
            await _transaction.DisposeAsync();
        }

        private async Task EndWithoutCommitAsync(CancellationToken cancellationToken, bool reportRollbackFailure = false)
        {
            _ended = true;
            try
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
            catch (Exception) when (!reportRollbackFailure && !cancellationToken.IsCancellationRequested)
            {
                // The transaction is already unusable (for example a failed commit); disposing it discards the work.
            }
            finally
            {
                // The undo must run even when the request was cancelled or the rollback failed.
                await _steps.RunOnceAsync();
            }
        }

        private sealed class UndoSteps
        {
            private readonly List<Func<CancellationToken, Task>> _undo = new();
            private bool _done;

            public void Add(Func<CancellationToken, Task> step) => _undo.Add(step);

            public void Discard() => _done = true;

            public async Task RunOnceAsync()
            {
                if (_done)
                    return;

                _done = true;
                foreach (var step in _undo)
                    await step(CancellationToken.None);
            }
        }
    }
}
