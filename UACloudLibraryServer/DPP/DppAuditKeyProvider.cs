using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Resolves the audit authentication key from configuration.
    /// </summary>
    /// <remarks>
    /// Deliberately configuration-only, with no database fallback: a key stored alongside the rows
    /// it authenticates would be readable by exactly the attacker it is meant to stop, which would
    /// make the keying theatre rather than protection. When no key is configured the log degrades to
    /// an unkeyed integrity check and a warning is logged, rather than silently appearing stronger
    /// than it is.
    /// </remarks>
    public class DppAuditKeyProvider : IDppAuditKeyProvider
    {
        public const string AuditKeyConfigurationPath = "Dpp:Audit:HmacKey";

        /// <summary>
        /// Keys that have been rotated out but must stay available so entries signed with them remain
        /// verifiable. Indexed: <c>Dpp:Audit:RetiredHmacKeys:0</c>, <c>:1</c>, ...
        /// </summary>
        public const string RetiredKeysConfigurationPath = "Dpp:Audit:RetiredHmacKeys";

        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly object _gate = new();
        private byte[] _cachedKey;
        private string _cachedKeyId;
        private bool _resolved;
        private Dictionary<string, byte[]> _keysById;

        public DppAuditKeyProvider(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger("DppAuditKeyProvider");
        }

        /// <summary>
        /// Stable identifier for a key, derived from the key itself so it cannot be mislabelled.
        /// </summary>
        /// <remarks>
        /// This is a truncated SHA-256 of the key bytes. It is published in the audit table, so it
        /// must not weaken the key: a 128-bit hash of a >=256-bit secret is not invertible, and the
        /// value only has to distinguish the handful of keys a deployment ever uses.
        /// </remarks>
        internal static string ComputeKeyId(byte[] key)
        {
            if (key is null || key.Length == 0)
            {
                return null;
            }

            return Convert.ToHexString(SHA256.HashData(key).AsSpan(0, 16));
        }

        public Task<byte[]> GetAuditKeyAsync(CancellationToken cancellationToken = default)
        {
            EnsureResolved();
            return Task.FromResult(_cachedKey);
        }

        public Task<DppAuditKey> GetCurrentKeyAsync(CancellationToken cancellationToken = default)
        {
            EnsureResolved();
            return Task.FromResult(new DppAuditKey(_cachedKey, _cachedKeyId));
        }

        public Task<DppAuditKeyLookup> TryGetKeyByIdAsync(string keyId, CancellationToken cancellationToken = default)
        {
            // A null key id is not a lookup failure: it records an entry written before any key was
            // configured, which is verified with a bare hash. Treating it as "key missing" would make
            // enabling a key for the first time look like wholesale tampering.
            if (string.IsNullOrEmpty(keyId))
            {
                return Task.FromResult(new DppAuditKeyLookup(true, null));
            }

            EnsureResolved();

            return Task.FromResult(_keysById.TryGetValue(keyId, out byte[] key)
                ? new DppAuditKeyLookup(true, key)
                : new DppAuditKeyLookup(false, null));
        }

        private void EnsureResolved()
        {
            if (_resolved)
            {
                return;
            }

            lock (_gate)
            {
                if (_resolved)
                {
                    return;
                }

                var keysById = new Dictionary<string, byte[]>(StringComparer.Ordinal);

                string configured = _configuration?[AuditKeyConfigurationPath];
                if (string.IsNullOrWhiteSpace(configured))
                {
                    _logger.LogWarning(
                        "{ConfigPath} is not configured, so DPP audit entries are hashed but not authenticated. " +
                        "The chain then only detects accidental corruption: anyone able to write to the audit tables can " +
                        "recompute the digests and pass verification. Configure a key from a managed secret store to make " +
                        "the log tamper-evident against database modification.",
                        AuditKeyConfigurationPath);

                    _cachedKey = null;
                    _cachedKeyId = null;
                }
                else
                {
                    _cachedKey = DecodeKey(configured, AuditKeyConfigurationPath);
                    _cachedKeyId = ComputeKeyId(_cachedKey);
                    keysById[_cachedKeyId] = _cachedKey;
                }

                // Retired keys keep previously written entries verifiable after a rotation. Without
                // them, rotating the key would make the whole existing chain fail verification and the
                // audit health check would report tampering that never occurred.
                IConfigurationSection retired = _configuration?.GetSection(RetiredKeysConfigurationPath);
                if (retired is not null)
                {
                    foreach (IConfigurationSection child in retired.GetChildren())
                    {
                        if (string.IsNullOrWhiteSpace(child.Value))
                        {
                            continue;
                        }

                        byte[] retiredKey = DecodeKey(child.Value, child.Path);
                        keysById[ComputeKeyId(retiredKey)] = retiredKey;
                    }
                }

                _keysById = keysById;
                _resolved = true;
            }
        }

        private static byte[] DecodeKey(string configured, string configurationPath)
        {
            byte[] key;
            try
            {
                key = Convert.FromBase64String(configured);
            }
            catch (FormatException)
            {
                // Falling back to unkeyed here would quietly weaken the log, so refuse instead.
                throw new InvalidOperationException(
                    $"'{configurationPath}' is not valid base64. Supply a base64-encoded key of at least 32 bytes.");
            }

            if (key.Length < 32)
            {
                throw new InvalidOperationException(
                    $"'{configurationPath}' must decode to at least 32 bytes to key HMAC-SHA256; got {key.Length}.");
            }

            return key;
        }
    }
}
