using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Resolves the ESDC signing key from configuration, falling back to a key persisted in the
    /// database, and generating one only when neither exists.
    /// </summary>
    /// <remarks>
    /// Outside the Development environment the database fallback is refused unless it is explicitly
    /// enabled via <see cref="AllowGeneratedKeyConfigurationPath"/>. Signing production ESDCs with a
    /// key held in application data is a deliberate downgrade, so it must be a decision an operator
    /// made on purpose rather than one the server made silently on their behalf.
    /// </remarks>
    public class EsdcSigningKeyProvider : IEsdcSigningKeyProvider
    {
        public const string PrivateKeyConfigurationPath = "Dpp:Esdc:PrivateKeyPem";
        public const string AllowGeneratedKeyConfigurationPath = "Dpp:Esdc:AllowGeneratedSigningKey";

        private const int KeySizeBits = 2048;

        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger _logger;

        public EsdcSigningKeyProvider(
            AppDbContext db,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILoggerFactory loggerFactory)
        {
            _db = db;
            _configuration = configuration;
            _environment = environment;
            _logger = loggerFactory.CreateLogger("EsdcSigningKeyProvider");
        }

        public async Task<string> GetOrCreatePrivateKeyPemAsync(CancellationToken cancellationToken = default)
        {
            // 1. An explicitly configured key wins. This is the recommended production arrangement:
            //    the secret lives in a managed store and never touches application data.
            string configured = _configuration?[PrivateKeyConfigurationPath];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            // Refuse the database fallback in production unless the operator opted in. Checked before
            // any key is read or generated so a rejected deployment never leaves signing material
            // behind in the database.
            if (!IsGeneratedKeyAllowed())
            {
                throw new InvalidOperationException(
                    $"No ESDC signing key is configured. Set '{PrivateKeyConfigurationPath}' from a managed secret store " +
                    $"(see the signing key management section of dpp.md), or set " +
                    $"'{AllowGeneratedKeyConfigurationPath}' to true to accept a server-generated key stored in the database. " +
                    "The server refuses to sign with a database-held key in this environment because that key is present " +
                    "in every database backup and readable by anything with database access.");
            }

            // 2. A key this deployment generated earlier. Reusing it is what keeps previously issued
            //    ESDCs verifiable after a restart.
            EsdcSigningKey existing = await _db.EsdcSigningKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == EsdcSigningKey.SingletonId, cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(existing?.PrivateKeyPem))
            {
                return existing.PrivateKeyPem;
            }

            // 3. Nothing available: generate once and persist.
            using var rsa = RSA.Create(KeySizeBits);
            string generated = rsa.ExportPkcs8PrivateKeyPem();

            var record = new EsdcSigningKey {
                Id = EsdcSigningKey.SingletonId,
                PrivateKeyPem = generated,
                CreatedAt = DateTimeOffset.UtcNow
            };

            try
            {
                _db.EsdcSigningKeys.Add(record);
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogWarning(
                    "No ESDC signing key was configured ({ConfigPath}), so one was generated and stored in the database. " +
                    "It is now shared by all instances and survives restarts, but it is also present in database backups. " +
                    "For production, supply a key from a managed secret store instead - see the signing key management section of dpp.md.",
                    PrivateKeyConfigurationPath);

                return generated;
            }
            catch (DbUpdateException)
            {
                // Another instance inserted the singleton row first. The fixed primary key makes that
                // collide rather than create a second, competing key - so discard ours and adopt theirs,
                // otherwise the two instances would sign with different keys.
                _db.ChangeTracker.Clear();

                EsdcSigningKey winner = await _db.EsdcSigningKeys
                    .AsNoTracking()
                    .FirstOrDefaultAsync(k => k.Id == EsdcSigningKey.SingletonId, cancellationToken)
                    .ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(winner?.PrivateKeyPem))
                {
                    throw;
                }

                _logger.LogInformation("Adopted the ESDC signing key generated concurrently by another instance.");
                return winner.PrivateKeyPem;
            }
        }

        // Development gets a working server with no setup; anywhere else the operator must say so.
        private bool IsGeneratedKeyAllowed()
        {
            if (_environment?.IsDevelopment() == true)
            {
                return true;
            }

            return _configuration?.GetValue<bool>(AllowGeneratedKeyConfigurationPath) == true;
        }
    }
}
