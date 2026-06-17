using System.Collections.Generic;

using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Models;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="DppPagination.TrySlice"/>. The slicing algorithm backs the
    /// EN 18222 <c>ReadDPPIdsByProductIds</c> endpoint. These tests pin the regressions that
    /// motivated the PR-review fixes: the result must be deterministic across calls (sorted
    /// ordinal, duplicates removed), the cursor must reject malformed values rather than silently
    /// resetting the client, an over-large cursor must clamp to a terminal empty page, and the
    /// <see cref="Pagination"/> envelope must only be emitted when paging was actually requested
    /// (so non-paged callers don't see <c>"pagination": null</c> on the wire).
    /// </summary>
    /// <remarks>
    /// Pure unit tests by design: no <c>WebApplicationFactory</c>, no Postgres. Tests that need
    /// the live HTTP pipeline belong in <c>Tests/CloudLibClientTests</c>.
    /// </remarks>
    public class DppPaginationTests
    {
        // Hoisted to satisfy CA1861 (no inline constant array arguments to Assert.Equal).
        // The names reflect what each test expects so the call sites still read clearly.
        private static readonly string[] s_dedupedOrdinalSorted = { "MU", "alpha", "beta", "mu", "zeta" };
        private static readonly string[] s_aB = { "a", "b" };
        private static readonly string[] s_cD = { "c", "d" };
        private static readonly string[] s_bC = { "b", "c" };

        [Fact]
        public void TrySlice_DedupsAndSortsOrdinal_ForDeterministicPaging()
        {
            // The DPP service may return the same id multiple times when one DPP backs more than
            // one productId, and it does not guarantee order. The cursor only produces stable
            // pages if the underlying sequence is itself stable.
            var raw = new List<string> { "zeta", "alpha", "alpha", "mu", "beta", "MU" };

            DppPagination.SliceOutcome outcome = DppPagination.TrySlice(
                raw, limit: null, cursor: null, out List<string> page, out Pagination pagination, out string error);

            Assert.Equal(DppPagination.SliceOutcome.Success, outcome);
            Assert.Null(error);
            // Ordinal sort: uppercase precedes lowercase.
            Assert.Equal(s_dedupedOrdinalSorted, page);
            Assert.Null(pagination); // No limit and no cursor: caller did not ask for paging.
        }

        [Fact]
        public void TrySlice_DropsNullAndEmptyEntries()
        {
            var raw = new List<string> { "a", null, "", "b", null };

            DppPagination.SliceOutcome outcome = DppPagination.TrySlice(
                raw, limit: null, cursor: null, out List<string> page, out _, out _);

            Assert.Equal(DppPagination.SliceOutcome.Success, outcome);
            Assert.Equal(s_aB, page);
        }

        [Fact]
        public void TrySlice_LimitedFirstPage_EmitsNextCursorAndHasMore()
        {
            var raw = new List<string> { "c", "a", "b", "d" };

            DppPagination.SliceOutcome outcome = DppPagination.TrySlice(
                raw, limit: 2, cursor: null, out List<string> page, out Pagination pagination, out _);

            Assert.Equal(DppPagination.SliceOutcome.Success, outcome);
            Assert.Equal(s_aB, page);
            Assert.NotNull(pagination);
            Assert.True(pagination.HasMore);
            Assert.Equal("2", pagination.NextCursor); // Next cursor is the absolute index, not a delta.
            Assert.Equal(2, pagination.Limit);
        }

        [Fact]
        public void TrySlice_FollowUpPage_UsesCursorAsAbsoluteIndex()
        {
            var raw = new List<string> { "c", "a", "b", "d" }; // sorted -> [a, b, c, d]

            DppPagination.SliceOutcome outcome = DppPagination.TrySlice(
                raw, limit: 2, cursor: "2", out List<string> page, out Pagination pagination, out _);

            Assert.Equal(DppPagination.SliceOutcome.Success, outcome);
            Assert.Equal(s_cD, page);
            Assert.NotNull(pagination);
            // Exactly fills the page; with no remaining items there is no further cursor to emit.
            Assert.False(pagination.HasMore);
            Assert.Null(pagination.NextCursor);
        }

        [Fact]
        public void TrySlice_TerminalPageWithoutLimit_HasNoNextCursor()
        {
            // A cursor was supplied so Pagination must be emitted (the caller is paging), but
            // there is no limit cap so the whole tail is returned and HasMore is false.
            var raw = new List<string> { "a", "b", "c" };

            DppPagination.SliceOutcome outcome = DppPagination.TrySlice(
                raw, limit: null, cursor: "1", out List<string> page, out Pagination pagination, out _);

            Assert.Equal(DppPagination.SliceOutcome.Success, outcome);
            Assert.Equal(s_bC, page);
            Assert.NotNull(pagination);
            Assert.False(pagination.HasMore);
            Assert.Null(pagination.NextCursor);
            Assert.Null(pagination.Limit);
        }

        [Fact]
        public void TrySlice_OversizeCursor_ClampsToEmptyTerminalPage()
        {
            // An over-large cursor must NOT throw or wrap around; it returns an empty page so a
            // client that overshoots (e.g. concurrent deletions) gets a clean terminal response.
            var raw = new List<string> { "a", "b" };

            DppPagination.SliceOutcome outcome = DppPagination.TrySlice(
                raw, limit: 1, cursor: "99", out List<string> page, out Pagination pagination, out _);

            Assert.Equal(DppPagination.SliceOutcome.Success, outcome);
            Assert.Empty(page);
            Assert.NotNull(pagination);
            Assert.False(pagination.HasMore);
            Assert.Null(pagination.NextCursor);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("-1")]
        [InlineData("1.5")]
        public void TrySlice_MalformedCursor_ReturnsCursorMalformed(string cursor)
        {
            // Note: int.TryParse with NumberStyles.Integer tolerates leading/trailing whitespace
            // (e.g. " 1 "), which matches the previous controller behavior and is intentionally
            // not treated as malformed.
            var raw = new List<string> { "a", "b" };

            DppPagination.SliceOutcome outcome = DppPagination.TrySlice(
                raw, limit: 1, cursor: cursor, out List<string> page, out Pagination pagination, out string error);

            Assert.Equal(DppPagination.SliceOutcome.CursorMalformed, outcome);
            Assert.Null(page);
            Assert.Null(pagination);
            Assert.Equal("cursor must be a non-negative integer", error);
        }

        [Fact]
        public void TrySlice_NoLimitNoCursor_OmitsPaginationEnvelope()
        {
            // Non-paged callers must not receive a Pagination envelope; otherwise the response
            // shape forces clients to ignore a meaningless "pagination": null block.
            var raw = new List<string> { "a", "b" };

            DppPagination.SliceOutcome outcome = DppPagination.TrySlice(
                raw, limit: null, cursor: null, out List<string> page, out Pagination pagination, out _);

            Assert.Equal(DppPagination.SliceOutcome.Success, outcome);
            Assert.Equal(s_aB, page);
            Assert.Null(pagination);
        }

        [Fact]
        public void TrySlice_LimitWithNoRemainder_EmitsPaginationWithoutNextCursor()
        {
            // When the limit exactly matches the remaining count the page is full but there is
            // nothing left to fetch; HasMore is false and NextCursor is null. Pagination is still
            // emitted because the caller asked for paging.
            var raw = new List<string> { "a", "b" };

            DppPagination.SliceOutcome outcome = DppPagination.TrySlice(
                raw, limit: 2, cursor: null, out List<string> page, out Pagination pagination, out _);

            Assert.Equal(DppPagination.SliceOutcome.Success, outcome);
            Assert.Equal(s_aB, page);
            Assert.NotNull(pagination);
            Assert.False(pagination.HasMore);
            Assert.Null(pagination.NextCursor);
            Assert.Equal(2, pagination.Limit);
        }
    }
}
