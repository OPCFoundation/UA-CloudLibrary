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
            [Fact]
            public void Timestamp_IsTruncatedToMicroseconds()
            {
                // PostgreSQL's timestamptz stores microseconds. A value carrying sub-microsecond ticks
                // would hash differently before and after a round-trip, so truncation must happen before
                // hashing rather than being left to the database.
                var withSubMicrosecondTicks = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(12345);

                DateTimeOffset truncated = DppAuditLog.TruncateToMicroseconds(withSubMicrosecondTicks);

                Assert.Equal(0, truncated.Ticks % (TimeSpan.TicksPerMillisecond / 1000));
                Assert.True(truncated <= withSubMicrosecondTicks);
                Assert.True(withSubMicrosecondTicks - truncated < TimeSpan.FromMilliseconds(0.001));
            }

            [Fact]
            public void TruncatedTimestamp_SurvivesDatabaseRoundTripUnchanged()
            {
                // Simulates what PostgreSQL does on reload: a truncated value must be a fixed point, so
                // the hash recomputed during verification matches the hash stored at append time.
                var original = DppAuditLog.TruncateToMicroseconds(DateTimeOffset.UtcNow);
                DateTimeOffset afterReload = DppAuditLog.TruncateToMicroseconds(original);

                Assert.Equal(original, afterReload);

                DppAuditEntry entry = Entry("dpp-1", "materials", original);
                string atAppend = DppAuditLog.ComputeHash(entry, DppAuditLog.GenesisHash);

                entry.Timestamp = afterReload;
                string atVerify = DppAuditLog.ComputeHash(entry, DppAuditLog.GenesisHash);

                Assert.Equal(atAppend, atVerify);
            }

            [Fact]
            public void UntruncatedTimestamp_WouldBreakVerification()
            {
                // Guards the regression itself: hashing an untruncated value and verifying against the
                // rounded value the database returns produces a mismatch, which is what this fix prevents.
                var untruncated = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(12345);

                DppAuditEntry entry = Entry("dpp-1", "materials", untruncated);
                string hashedBeforeStore = DppAuditLog.ComputeHash(entry, DppAuditLog.GenesisHash);

                entry.Timestamp = DppAuditLog.TruncateToMicroseconds(untruncated);
                string hashedAfterReload = DppAuditLog.ComputeHash(entry, DppAuditLog.GenesisHash);

                Assert.NotEqual(hashedBeforeStore, hashedAfterReload);
            }

            [Fact]
            public void KeyedHash_DiffersFromUnkeyedHash()
            {
                // The point of keying: an unkeyed digest is a public function of the stored rows, so
                // anyone able to edit the audit tables can recompute it. A keyed digest cannot be
                // reproduced without the key.
                var timestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
                DppAuditEntry entry = Entry("dpp-1", "materials", timestamp);
                byte[] key = new byte[32];

                Assert.NotEqual(
                    DppAuditLog.ComputeHash(entry, DppAuditLog.GenesisHash),
                    DppAuditLog.ComputeHash(entry, DppAuditLog.GenesisHash, key));
            }

            [Fact]
            public void DifferentKeys_ProduceDifferentHashes()
            {
                // An attacker with a different key (or none) cannot forge a digest that verifies.
                var timestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
                DppAuditEntry entry = Entry("dpp-1", "materials", timestamp);

                byte[] keyA = new byte[32];
                byte[] keyB = new byte[32];
                keyB[0] = 1;

                Assert.NotEqual(
                    DppAuditLog.ComputeHash(entry, DppAuditLog.GenesisHash, keyA),
                    DppAuditLog.ComputeHash(entry, DppAuditLog.GenesisHash, keyB));
            }

            [Fact]
            public void SameKey_ProducesStableHash()
            {
                // Verification recomputes with the same key, so it must be deterministic.
                var timestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
                DppAuditEntry entry = Entry("dpp-1", "materials", timestamp);
                byte[] key = new byte[32];

                Assert.Equal(
                    DppAuditLog.ComputeHash(entry, DppAuditLog.GenesisHash, key),
                    DppAuditLog.ComputeHash(entry, DppAuditLog.GenesisHash, key));
            }

            [Fact]
            public void KeyedHash_StillDetectsFieldCollisions()
            {
                // The length-prefixed encoding must keep working under HMAC, not just bare SHA-256.
                var timestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
                byte[] key = new byte[32];

                Assert.NotEqual(
                    DppAuditLog.ComputeHash(Entry("a|b", "c", timestamp), DppAuditLog.GenesisHash, key),
                    DppAuditLog.ComputeHash(Entry("a", "b|c", timestamp), DppAuditLog.GenesisHash, key));
            }
        }
    }
