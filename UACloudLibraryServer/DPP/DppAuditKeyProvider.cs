using System;
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

        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private byte[] _cachedKey;
        private bool _resolved;

        public DppAuditKeyProvider(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger("DppAuditKeyProvider");
        }

        public Task<byte[]> GetAuditKeyAsync(CancellationToken cancellationToken = default)
        {
            if (_resolved)
            {
                return Task.FromResult(_cachedKey);
            }

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
                _resolved = true;
                return Task.FromResult(_cachedKey);
            }

            try
            {
                _cachedKey = Convert.FromBase64String(configured);
            }
            catch (FormatException)
            {
                // Falling back to unkeyed here would quietly weaken the log, so refuse instead.
                throw new InvalidOperationException(
                    $"'{AuditKeyConfigurationPath}' is not valid base64. Supply a base64-encoded key of at least 32 bytes.");
            }

            if (_cachedKey.Length < 32)
            {
                throw new InvalidOperationException(
                    $"'{AuditKeyConfigurationPath}' must decode to at least 32 bytes to key HMAC-SHA256; got {_cachedKey.Length}.");
            }

            _resolved = true;
            return Task.FromResult(_cachedKey);
        }
    }
}
