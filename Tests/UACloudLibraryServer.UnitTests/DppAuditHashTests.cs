using System;

using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Models;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for the audit entry hash canonicalization. The encoding must be injective: two
    /// entries whose fields differ must never produce the same hash, or an entry could be altered
    /// while still passing chain verification.
    /// </summary>
    public class DppAuditHashTests
    {
        private static DppAuditEntry Entry(string dppId, string elementPath, DateTimeOffset timestamp) => new() {
            Timestamp = timestamp,
            OperatorId = "operator",
            Operation = DppAuditOperation.Modify,
            DppId = dppId,
            ElementPath = elementPath,
            Outcome = "Success"
        };

        [Fact]
        public void SeparatorInFieldValue_DoesNotCollide()
        {
            // The reported collision: with '|'-joined fields, DppId "a|b" + ElementPath "c" and
            // DppId "a" + ElementPath "b|c" produce the identical canonical string.
            var timestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            string first = DppAuditLog.ComputeHash(Entry("a|b", "c", timestamp), DppAuditLog.GenesisHash);
            string second = DppAuditLog.ComputeHash(Entry("a", "b|c", timestamp), DppAuditLog.GenesisHash);

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void NullAndEmptyFields_DoNotCollide()
        {
            // A null element path must not hash the same as an empty one; otherwise a whole-DPP
            // entry could be recast as an element-scoped entry without breaking the chain.
            var timestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            string nullPath = DppAuditLog.ComputeHash(Entry("dpp-1", null, timestamp), DppAuditLog.GenesisHash);
            string emptyPath = DppAuditLog.ComputeHash(Entry("dpp-1", string.Empty, timestamp), DppAuditLog.GenesisHash);

            Assert.NotEqual(nullPath, emptyPath);
        }

        [Fact]
        public void SameContent_ProducesStableHash()
        {
            // Verification recomputes the hash, so identical content must hash identically.
            var timestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            string first = DppAuditLog.ComputeHash(Entry("dpp-1", "materials", timestamp), DppAuditLog.GenesisHash);
            string second = DppAuditLog.ComputeHash(Entry("dpp-1", "materials", timestamp), DppAuditLog.GenesisHash);

            Assert.Equal(first, second);
        }

        [Fact]
        public void DifferentPreviousHash_ChangesEntryHash()
        {
            // The chain link itself must be covered, or entries could be reordered freely.
            var timestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            DppAuditEntry entry = Entry("dpp-1", "materials", timestamp);

            string atGenesis = DppAuditLog.ComputeHash(entry, DppAuditLog.GenesisHash);
            string afterOther = DppAuditLog.ComputeHash(entry, new string('a', 64));

            Assert.NotEqual(atGenesis, afterOther);
        }
    }
}
