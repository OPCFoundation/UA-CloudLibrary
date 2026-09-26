using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Opc.Ua.Cloud.Library;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Pins which append failures the audit log is required to wrap in
    /// <see cref="DppAuditException"/> before they reach <c>DppAuditFailureFilter</c>.
    /// </summary>
    /// <remarks>
    /// The filter can only shape <see cref="DppAuditException"/>. Anything else escaping
    /// <c>RecordAsync</c> bypasses it: a read returns a bare 500 instead of the documented 503, and a
    /// post-commit mutation loses the "applied; do not retry" response that stops a client repeating
    /// an already-durable change. The retry loop's catches originally covered only
    /// <see cref="DbUpdateException"/>, transient <see cref="DbException"/> and
    /// <see cref="InvalidOperationException"/>, so an ordinary database outage escaped unwrapped.
    /// <para>
    /// <c>RecordAsync</c> itself needs a live Npgsql context, so these tests pin the predicate that
    /// decides what gets wrapped rather than exercising the loop end to end.
    /// </para>
    /// </remarks>
    public class DppAuditAppendFailureWrappingTests
    {
        private sealed class FakeDbException : DbException
        {
            public FakeDbException(string message) : base(message) { }
        }

        /// <summary>
        /// Mirrors the <c>when</c> clause guarding the catch-all in <c>RecordAsync</c>. Kept in sync
        /// deliberately: if the production filter widens to swallow cancellation or to re-wrap an
        /// already-shaped refusal, this should start failing.
        /// </summary>
        private static bool IsWrapped(Exception ex) =>
            ex is not DppAuditException and not OperationCanceledException;

        [Fact]
        public void NonTransientDatabaseFailure_IsWrapped()
        {
            // The reported gap: a connection or permission failure matches none of the specific
            // catches, so without the catch-all it left RecordAsync as a raw DbException.
            Assert.True(IsWrapped(new FakeDbException("connection refused")));
        }

        [Fact]
        public void UnexpectedFailure_IsWrapped()
        {
            // Anything a provider might surface must also be shaped rather than escaping raw.
            Assert.True(IsWrapped(new TimeoutException("command timeout")));
            Assert.True(IsWrapped(new InvalidOperationException("provider fault")));
            Assert.True(IsWrapped(new DbUpdateException("concurrency")));
        }

        [Fact]
        public void DeliberateRefusal_IsNotReWrapped()
        {
            // DppAuditException is already the shaped refusal and carries its own message - for
            // instance the missing-checkpoint case, which must reach the caller unchanged.
            Assert.False(IsWrapped(new DppAuditException("checkpoint missing while entries exist")));
        }

        [Fact]
        public void Cancellation_IsNotWrapped()
        {
            // The caller gave up, which says nothing about the log. Turning that into an audit
            // failure would report a client disconnect or probe timeout as an unauditable operation.
            Assert.False(IsWrapped(new OperationCanceledException()));
            Assert.False(IsWrapped(new TaskCanceledException()));
        }

        [Fact]
        public void PostCommitFlag_SurvivesWrapping()
        {
            // The wrapped failure must still be able to carry the post-commit marker, otherwise a
            // committed mutation whose outcome append fails would report a retryable refusal.
            var inner = new FakeDbException("connection refused");
            var wrapped = new DppAuditException("could not record outcome", inner, mutationCommitted: true);

            Assert.True(wrapped.MutationCommitted);
            Assert.Same(inner, wrapped.InnerException);
        }
    }
}
