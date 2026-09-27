using System;
using System.Text;

using Opc.Ua.Cloud.Library;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for the audit checkpoint MAC. Chain verification alone cannot detect truncation,
    /// and an unauthenticated checkpoint cannot either: an attacker who trims the log to a valid
    /// prefix can restate the count and tail to match it. These tests pin the property that makes
    /// that rewrite detectable, namely that the checkpoint cannot be re-MAC'd without the key.
    /// </summary>
    public class DppAuditCheckpointMacTests
    {
        private static byte[] Key(byte seed)
        {
            byte[] key = new byte[32];
            for (int i = 0; i < key.Length; i++)
            {
                key[i] = (byte)(seed + i);
            }

            return key;
        }

        [Fact]
        public void Mac_IsStableForTheSameInputs()
        {
            byte[] key = Key(1);

            string first = DppAuditLog.ComputeCheckpointMac(7, "ABC123", key);
            string second = DppAuditLog.ComputeCheckpointMac(7, "ABC123", key);

            Assert.NotNull(first);
            Assert.Equal(first, second);
        }

        [Fact]
        public void Mac_ChangesWhenTheCountIsRolledBack()
        {
            byte[] key = Key(1);

            // The truncation the reviewer described: keep a valid tail, lower the count.
            string genuine = DppAuditLog.ComputeCheckpointMac(10, "TAIL", key);
            string rolledBack = DppAuditLog.ComputeCheckpointMac(4, "TAIL", key);

            Assert.NotEqual(genuine, rolledBack);
        }

        [Fact]
        public void Mac_ChangesWhenTheTailIsReplacedWithAnEarlierEntryHash()
        {
            byte[] key = Key(1);

            string genuine = DppAuditLog.ComputeCheckpointMac(10, "TAIL_AT_10", key);
            string prefixTail = DppAuditLog.ComputeCheckpointMac(10, "TAIL_AT_4", key);

            Assert.NotEqual(genuine, prefixTail);
        }

        [Fact]
        public void Mac_CannotBeReproducedWithoutTheCorrectKey()
        {
            string withRealKey = DppAuditLog.ComputeCheckpointMac(5, "TAIL", Key(1));
            string withOtherKey = DppAuditLog.ComputeCheckpointMac(5, "TAIL", Key(9));

            Assert.NotEqual(withRealKey, withOtherKey);
        }

        [Fact]
        public void Mac_FieldsAreNotAmbiguouslyJoined()
        {
            byte[] key = Key(1);

            // Length-prefixing means a digit cannot be shifted between the count and the tail to
            // produce two different checkpoints with the same MAC.
            string a = DppAuditLog.ComputeCheckpointMac(1, "23TAIL", key);
            string b = DppAuditLog.ComputeCheckpointMac(12, "3TAIL", key);

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void Mac_IsNullWhenNoKeyIsConfigured()
        {
            // Without a key the checkpoint stays unauthenticated; this is a documented limitation
            // rather than silent success, so the method must not invent a value.
            Assert.Null(DppAuditLog.ComputeCheckpointMac(3, "TAIL", null));
        }
    }
}
