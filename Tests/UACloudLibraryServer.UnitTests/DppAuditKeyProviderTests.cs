using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using Opc.Ua.Cloud.Library;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for audit key identification and rotation.
    /// </summary>
    /// <remarks>
    /// Verification used to recompute every historical entry with whatever key was configured at
    /// verification time. Enabling a key for the first time, or rotating one, therefore made the
    /// entire existing chain fail and the audit health check report tampering that never happened.
    /// A false alarm on a tamper-evidence control is worse than none, because it trains operators to
    /// ignore the alert, so these tests pin the behaviour that prevents it.
    /// </remarks>
    public class DppAuditKeyProviderTests
    {
        private static string KeyOf(byte seed)
        {
            byte[] key = new byte[32];
            for (int i = 0; i < key.Length; i++)
            {
                key[i] = (byte)(seed + i);
            }

            return Convert.ToBase64String(key);
        }

        private static DppAuditKeyProvider Provider(string current, params string[] retired)
        {
            var settings = new Dictionary<string, string>();
            if (current is not null)
            {
                settings[DppAuditKeyProvider.AuditKeyConfigurationPath] = current;
            }

            for (int i = 0; i < retired.Length; i++)
            {
                settings[$"{DppAuditKeyProvider.RetiredKeysConfigurationPath}:{i}"] = retired[i];
            }

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            return new DppAuditKeyProvider(configuration, NullLoggerFactory.Instance);
        }

        [Fact]
        public async Task CurrentKey_CarriesAStableFingerprint()
        {
            DppAuditKey first = await Provider(KeyOf(1)).GetCurrentKeyAsync();
            DppAuditKey second = await Provider(KeyOf(1)).GetCurrentKeyAsync();

            Assert.NotNull(first.KeyId);
            Assert.Equal(first.KeyId, second.KeyId);
        }

        [Fact]
        public async Task DifferentKeys_ProduceDifferentFingerprints()
        {
            DppAuditKey a = await Provider(KeyOf(1)).GetCurrentKeyAsync();
            DppAuditKey b = await Provider(KeyOf(50)).GetCurrentKeyAsync();

            Assert.NotEqual(a.KeyId, b.KeyId);
        }

        [Fact]
        public async Task NoKeyConfigured_YieldsNullKeyAndNullFingerprint()
        {
            DppAuditKey key = await Provider(null).GetCurrentKeyAsync();

            Assert.Null(key.Key);
            Assert.Null(key.KeyId);
        }

        [Fact]
        public async Task NullKeyId_ResolvesAsFoundWithNoKey()
        {
            // Entries written before any key was configured carry a null id. Treating that as a
            // missing key would make enabling a key for the first time look like mass tampering.
            DppAuditKeyLookup lookup = await Provider(KeyOf(1)).TryGetKeyByIdAsync(null);

            Assert.True(lookup.Found);
            Assert.Null(lookup.Key);
        }

        [Fact]
        public async Task RetiredKey_StaysResolvableAfterRotation()
        {
            // The rotation case: entries were signed with the old key, which is now retired.
            string oldKey = KeyOf(1);
            string newKey = KeyOf(90);

            string oldKeyId = (await Provider(oldKey).GetCurrentKeyAsync()).KeyId;

            DppAuditKeyProvider rotated = Provider(newKey, oldKey);
            DppAuditKeyLookup lookup = await rotated.TryGetKeyByIdAsync(oldKeyId);

            Assert.True(lookup.Found);
            Assert.Equal(Convert.FromBase64String(oldKey), lookup.Key);
        }

        [Fact]
        public async Task CurrentKey_IsResolvableByItsOwnFingerprint()
        {
            DppAuditKeyProvider provider = Provider(KeyOf(1));
            DppAuditKey current = await provider.GetCurrentKeyAsync();

            DppAuditKeyLookup lookup = await provider.TryGetKeyByIdAsync(current.KeyId);

            Assert.True(lookup.Found);
            Assert.Equal(current.Key, lookup.Key);
        }

        [Fact]
        public async Task UnknownKeyId_IsReportedAsNotFound()
        {
            // Must be distinguishable from "verified" AND from "tampered": the caller turns this into
            // an explicit "unverifiable" signal rather than a tampering claim.
            DppAuditKeyLookup lookup = await Provider(KeyOf(1)).TryGetKeyByIdAsync("DEADBEEFDEADBEEFDEADBEEFDEADBEEF");

            Assert.False(lookup.Found);
            Assert.Null(lookup.Key);
        }

        [Fact]
        public async Task RotatingAwayWithoutRetainingTheOldKey_MakesOldEntriesUnverifiable()
        {
            // Documents the residual limitation: dropping a retired key is not recoverable, and the
            // old entries become unverifiable rather than silently passing.
            string oldKeyId = (await Provider(KeyOf(1)).GetCurrentKeyAsync()).KeyId;

            DppAuditKeyLookup lookup = await Provider(KeyOf(90)).TryGetKeyByIdAsync(oldKeyId);

            Assert.False(lookup.Found);
        }

        [Theory]
        [InlineData("not base64!!")]
        public async Task MalformedKey_IsRefusedRatherThanSilentlyIgnored(string configured)
        {
            // Falling back to unkeyed would quietly weaken the log.
            DppAuditKeyProvider provider = Provider(configured);
            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetCurrentKeyAsync());
        }

        [Fact]
        public async Task ShortKey_IsRefused()
        {
            string tooShort = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

            DppAuditKeyProvider provider = Provider(tooShort);
            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetCurrentKeyAsync());
        }
    }
}
