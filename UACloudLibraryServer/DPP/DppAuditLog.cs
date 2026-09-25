using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// EF-backed, hash-chained audit log. Each entry's hash covers its content plus the previous
    /// hash, making the log tamper-evident (EN 18246 §4.7). Appends run inside a serializable
    /// transaction that also advances a checkpoint row, so concurrent writers - including writers in
    /// other application instances - cannot branch the chain, and truncation of trailing entries is
    /// detectable.
    /// </summary>
    public class DppAuditLog : IDppAuditLog
    {
        internal const string GenesisHash = "0000000000000000000000000000000000000000000000000000000000000000";

        // Serializes appends within this process. This is an optimization that avoids most
        // transaction retries; correctness across instances comes from the serializable transaction
        // and the checkpoint update, not from this lock.
        private static readonly SemaphoreSlim s_appendLock = new(1, 1);

        private const int MaxAttempts = 5;

        /// <summary>
        /// Opt-in that allows a single keyed append to adopt an unkeyed checkpoint, for deployments
        /// enabling <see cref="DppAuditKeyProvider.AuditKeyConfigurationPath"/> over an existing
        /// unkeyed log.
        /// </summary>
        /// <remarks>
        /// This cannot be inferred from the data. The previous heuristic - "no entry carries a
        /// KeyId, so the log must predate the key" - reads attacker-controlled columns: KeyId lives
        /// in the same table the HMAC exists to protect, so anyone able to rewrite history can also
        /// clear every KeyId and null the checkpoint's key and MAC, making the log look legitimately
        /// legacy. The next honest append would then re-MAC the forged chain with the current key.
        /// Requiring an explicit operator decision removes the forgeable inference.
        /// </remarks>
        public const string AllowUnkeyedCheckpointMigrationPath = "Dpp:Audit:AllowUnkeyedCheckpointMigration";

        private readonly AppDbContext _db;
        private readonly IDppAuditKeyProvider _keyProvider;
        private readonly ILogger _logger;
        private readonly bool _allowUnkeyedCheckpointMigration;

        public DppAuditLog(AppDbContext db, IDppAuditKeyProvider keyProvider, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _db = db;
            _keyProvider = keyProvider;
            _logger = loggerFactory.CreateLogger("DppAuditLog");
            _allowUnkeyedCheckpointMigration =
                configuration?.GetValue<bool>(AllowUnkeyedCheckpointMigrationPath) ?? false;
        }

        /// <summary>
        /// Appends an audit entry, retrying on transient serialization conflicts. Throws
        /// <see cref="DppAuditException"/> when the entry cannot be durably committed: callers must
        /// fail the originating operation rather than proceed with an unlogged access or change,
        /// because silently dropping the record would forfeit the non-repudiation guarantee.
        /// </summary>
        public async Task RecordAsync(string operatorId, DppAuditOperation operation, string dppId, string elementPath, string outcome, string operationId = null)
        {
            Exception lastError = null;

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                await s_appendLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    await AppendOnceAsync(operatorId, operation, dppId, elementPath, outcome, operationId).ConfigureAwait(false);
                    return;
                }
                catch (DbUpdateException ex)
                {
                    // Covers DbUpdateConcurrencyException: another writer advanced the chain between
                    // our read and our commit. Reload and retry.
                    lastError = ex;
                    ResetTracking();
                }
                catch (DbException ex) when (IsTransientConflict(ex))
                {
                    // Under Serializable isolation PostgreSQL often detects the conflict at COMMIT,
                    // where the provider raises a bare DbException (SQLSTATE 40001) rather than a
                    // DbUpdateException. Without this the expected cross-instance conflict would
                    // escape the retry loop and surface as an unshaped 500 instead of retrying or
                    // returning the documented 503.
                    lastError = ex;
                    ResetTracking();
                }
                catch (InvalidOperationException ex)
                {
                    // Note DppAuditException derives from Exception, not InvalidOperationException,
                    // so a deliberate refusal (e.g. a missing checkpoint over existing entries)
                    // propagates immediately instead of being retried five times.
                    lastError = ex;
                    ResetTracking();
                }
                finally
                {
                    s_appendLock.Release();
                }

                if (attempt < MaxAttempts)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(20 * attempt)).ConfigureAwait(false);
                }
            }

            _logger.LogError(
                lastError,
                "Failed to append DPP audit entry for {Operation} on {DppId} after {Attempts} attempts; failing the operation to preserve non-repudiation.",
                operation,
                dppId,
                MaxAttempts);

            throw new DppAuditException(
                $"Could not durably record the {operation} audit entry for DPP '{dppId}'. The operation was refused because it cannot be audited.",
                lastError);
        }

        // PostgreSQL SQLSTATEs for conflicts that are expected under Serializable isolation and are
        // resolved by retrying: 40001 serialization_failure, 40P01 deadlock_detected.
        private const string SerializationFailureSqlState = "40001";
        private const string DeadlockDetectedSqlState = "40P01";

        /// <summary>
        /// True when the exception (or any exception it wraps) reports a SQLSTATE that indicates a
        /// transient concurrency conflict rather than a genuine failure.
        /// </summary>
        /// <remarks>
        /// Matched on SQLSTATE rather than by catching <c>Npgsql.PostgresException</c> so the audit
        /// log does not take a direct dependency on the database provider package. EF frequently
        /// wraps the provider exception, so inner exceptions are inspected as well.
        /// </remarks>
        private static bool IsTransientConflict(Exception exception)
        {
            for (Exception current = exception; current is not null; current = current.InnerException)
            {
                if (current is DbException dbException
                    && (string.Equals(dbException.SqlState, SerializationFailureSqlState, StringComparison.Ordinal)
                        || string.Equals(dbException.SqlState, DeadlockDetectedSqlState, StringComparison.Ordinal)))
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetTracking()
        {
            // Drop tracked state so the next attempt re-reads the current tail instead of replaying stale values.
            _db.ChangeTracker.Clear();
        }

        private async Task AppendOnceAsync(string operatorId, DppAuditOperation operation, string dppId, string elementPath, string outcome, string operationId)
        {
            // AppDbContext enables EnableRetryOnFailure, and NpgsqlRetryingExecutionStrategy refuses
            // user-initiated transactions: a retry would otherwise replay only part of the unit. The
            // transaction must therefore be opened *by* the strategy so the whole read-hash-write
            // sequence is one retriable unit.
            IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () => {
                // Serializable isolation prevents two instances from reading the same tail and writing
                // entries that claim the same predecessor.
                using var transaction = await _db.Database
                    .BeginTransactionAsync(System.Data.IsolationLevel.Serializable)
                    .ConfigureAwait(false);

                // The strategy may replay this delegate, so any state tracked by a failed attempt has
                // to be discarded before the tail is re-read.
                _db.ChangeTracker.Clear();

                DppAuditEntry tail = await _db.DppAuditEntries
                    .OrderByDescending(e => e.Sequence)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);

                string previousHash = tail?.EntryHash ?? GenesisHash;

                var entry = new DppAuditEntry {
                    // PostgreSQL's "timestamp with time zone" stores microseconds, but DateTimeOffset.UtcNow
                    // carries 100-nanosecond ticks. Hashing the untruncated value would produce a hash the
                    // database can never reproduce: on reload the timestamp comes back rounded, so
                    // VerifyChainAsync would recompute a different hash and report tampering for entries
                    // that were never touched. Truncate first so the hashed value is the value stored.
                    Timestamp = TruncateToMicroseconds(DateTimeOffset.UtcNow),
                    OperatorId = string.IsNullOrEmpty(operatorId) ? "anonymous" : operatorId,
                    Operation = operation,
                    DppId = dppId,
                    ElementPath = elementPath,
                    Outcome = outcome,
                    OperationId = operationId,
                    PreviousHash = previousHash
                };
                DppAuditKey auditKey = await _keyProvider.GetCurrentKeyAsync().ConfigureAwait(false);
                entry.EntryHash = ComputeHash(entry, previousHash, auditKey.Key);

                // Record which key signed this entry so verification can later use that key rather
                // than whatever is configured at verification time.
                entry.KeyId = auditKey.KeyId;

                _db.DppAuditEntries.Add(entry);

                DppAuditCheckpoint checkpoint = await _db.DppAuditCheckpoints
                    .FirstOrDefaultAsync(c => c.Id == DppAuditCheckpoint.SingletonId)
                    .ConfigureAwait(false);

                if (checkpoint is null)
                {
                    // A missing checkpoint is only legitimate when nothing has ever been appended.
                    // With entries already present it means the checkpoint was deleted - and creating
                    // a fresh one here would MAC it over whatever prefix survives, so the next
                    // legitimate append would permanently authenticate an attacker's truncation
                    // before the health check ever observed the checkpoint was gone. Refuse instead:
                    // the append fails, the calling operation is refused (DppAuditException), and the
                    // missing checkpoint stays visible for VerifyChainAsync to report.
                    long existing = await _db.DppAuditEntries.LongCountAsync().ConfigureAwait(false);

                    // The new entry is tracked but not yet committed, so it is not counted here.
                    if (existing > 0)
                    {
                        _logger.LogCritical(
                            "DPP audit checkpoint is missing while audit entries exist. Refusing to append, because initializing a new checkpoint would authenticate a possibly-truncated log. Restore the checkpoint from backup or re-initialize the audit log deliberately.");

                        throw new DppAuditException(
                            "The DPP audit checkpoint is missing while audit entries exist. The log may have been truncated, " +
                            "so appending would authenticate an unverified state. Explicit operator recovery is required.");
                    }

                    checkpoint = new DppAuditCheckpoint {
                        Id = DppAuditCheckpoint.SingletonId,
                        EntryCount = existing + 1
                    };
                    _db.DppAuditCheckpoints.Add(checkpoint);
                }
                else
                {
                    // Extending a checkpoint asserts that it described the log correctly up to this
                    // point - and re-MACs that assertion with the real key. So it has to be checked
                    // first. Otherwise an attacker truncates the chain, sets EntryCount to the
                    // surviving length, and simply waits: the next legitimate append overwrites the
                    // tail, signs the attacker's count, and verification then accepts the truncated
                    // history as authentic. Refusing to build on an unverified checkpoint is what
                    // stops a normal write from laundering the tampering.
                    await EnsureCheckpointMatchesLogAsync(checkpoint, auditKey).ConfigureAwait(false);

                    checkpoint.EntryCount += 1;
                }

                checkpoint.TailHash = entry.EntryHash;
                checkpoint.UpdatedAt = entry.Timestamp;
                checkpoint.CheckpointMac = ComputeCheckpointMac(checkpoint.EntryCount, checkpoint.TailHash, auditKey.Key, auditKey.KeyId);
                checkpoint.KeyId = auditKey.KeyId;

                await _db.SaveChangesAsync().ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Refuses the append unless the existing checkpoint already describes the persisted log.
        /// </summary>
        /// <remarks>
        /// This is a guard against laundering, not a full chain verification: it deliberately does
        /// not re-walk every entry hash, because doing so on every append would make each write cost
        /// a full table scan. It checks the three fields an append is about to overwrite and re-sign
        /// &#8212; the count, the tail, and the MAC binding them &#8212; so a checkpoint that has been
        /// edited to describe a truncated log cannot be extended and thereby authenticated. Detecting
        /// alterations *within* the retained entries remains <see cref="VerifyChainAsync"/>'s job.
        /// </remarks>
        /// <exception cref="DppAuditException">
        /// The checkpoint does not match the log, so appending would sign an unverified state.
        /// </exception>
        private async Task EnsureCheckpointMatchesLogAsync(DppAuditCheckpoint checkpoint, DppAuditKey auditKey)
        {
            // Excludes the new entry: it is tracked but not yet persisted.
            long persistedCount = await _db.DppAuditEntries
                .AsNoTracking()
                .LongCountAsync()
                .ConfigureAwait(false);

            if (checkpoint.EntryCount != persistedCount)
            {
                _logger.LogCritical(
                    "DPP audit checkpoint count does not match the persisted log; refusing to append so the mismatch is not signed away.");

                throw new DppAuditException(
                    "The DPP audit checkpoint does not match the persisted log, so the log may have been truncated or altered. " +
                    "Appending would authenticate that state. Explicit operator recovery is required.");
            }

            DppAuditEntry persistedTail = await _db.DppAuditEntries
                .AsNoTracking()
                .OrderByDescending(e => e.Sequence)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            string expectedTail = persistedTail?.EntryHash ?? GenesisHash;
            if (!string.Equals(checkpoint.TailHash, expectedTail, StringComparison.Ordinal))
            {
                _logger.LogCritical(
                    "DPP audit checkpoint tail hash does not match the persisted log; refusing to append so the mismatch is not signed away.");

                throw new DppAuditException(
                    "The DPP audit checkpoint tail does not match the persisted log, so the most recent entries may have been replaced. " +
                    "Appending would authenticate that state. Explicit operator recovery is required.");
            }

            // With no key the MAC is absent by design and there is nothing further to check: the
            // checkpoint was never authenticated, which the README documents as the unkeyed mode.
            if (auditKey.Key is null)
            {
                return;
            }

            // A keyed deployment must not extend an unkeyed or wrongly-keyed checkpoint: that is the
            // downgrade path VerifyChainAsync already refuses, and it must not be reachable here either.
            DppAuditKeyLookup checkpointKey = await _keyProvider.TryGetKeyByIdAsync(checkpoint.KeyId).ConfigureAwait(false);
            if (!checkpointKey.Found || checkpointKey.Key is null)
            {
                // Enabling a key over an existing unkeyed log is the one legitimate reason to adopt
                // an unkeyed checkpoint, but it cannot be detected from the log itself: KeyId is a
                // plain column in the very table the key protects, so an attacker who rewrites
                // history can also clear every KeyId and the checkpoint's key/MAC and make a forged
                // chain look like untouched pre-key history. Inferring "legacy" from that data would
                // let the next honest append re-MAC the forgery. The migration must therefore be an
                // explicit operator decision.
                if (checkpoint.KeyId is null && _allowUnkeyedCheckpointMigration)
                {
                    _logger.LogWarning(
                        "DPP audit checkpoint is unkeyed and is being re-keyed by this append because {ConfigPath} is enabled. " +
                        "Entries written before the key was configured remain unauthenticated. Disable this setting once the migration is complete.",
                        AllowUnkeyedCheckpointMigrationPath);
                    return;
                }

                _logger.LogCritical(
                    "DPP audit checkpoint is unkeyed or signed with an unavailable key while keyed auditing is enabled; refusing to append.");

                throw new DppAuditException(
                    "The DPP audit checkpoint is not authenticated with an available key while keyed auditing is enabled. " +
                    "Appending would authenticate an unverified state. If this deployment is enabling an audit key over an " +
                    $"existing unkeyed log, set '{AllowUnkeyedCheckpointMigrationPath}' to true for the migration. " +
                    "Otherwise explicit operator recovery is required.");
            }

            string expectedMac = ComputeCheckpointMac(checkpoint.EntryCount, checkpoint.TailHash, checkpointKey.Key, checkpoint.KeyId);
            if (string.IsNullOrEmpty(checkpoint.CheckpointMac)
                || !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(checkpoint.CheckpointMac),
                    Encoding.UTF8.GetBytes(expectedMac)))
            {
                _logger.LogCritical(
                    "DPP audit checkpoint MAC does not verify; refusing to append so the altered checkpoint is not re-signed with the current key.");

                throw new DppAuditException(
                    "The DPP audit checkpoint MAC does not verify, so the checkpoint has been altered. " +
                    "Appending would re-sign it with the current key. Explicit operator recovery is required.");
            }
        }

        /// <summary>
        /// Recomputes the hash chain and compares it against the persisted checkpoint. Returns false
        /// when any entry was altered or inserted, and also when entries were removed from the end -
        /// a case the chain alone cannot reveal, since a truncated log is still a valid prefix.
        /// </summary>
        public async Task<bool> VerifyChainAsync(CancellationToken cancellationToken = default)
        {
            // AppDbContext enables EnableRetryOnFailure, and NpgsqlRetryingExecutionStrategy rejects
            // user-initiated transactions, so the snapshot below has to be opened by the strategy.
            IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(
                cancellationToken,
                (token) => VerifyChainInSnapshotAsync(token)).ConfigureAwait(false);
        }

        private async Task<bool> VerifyChainInSnapshotAsync(CancellationToken cancellationToken)
        {
            // The entries and the checkpoint must come from the same snapshot. Read separately under
            // the default read-committed isolation, an append committing between the two queries would
            // pair the old entry list with the advanced checkpoint, and verification would report
            // tampering for a chain that is actually valid. RepeatableRead pins both reads to one
            // snapshot so a concurrent AppendOnceAsync cannot produce a spurious failure.
            await using var snapshot = await _db.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken)
                .ConfigureAwait(false);

            List<DppAuditEntry> entries = await _db.DppAuditEntries
                .AsNoTracking()
                .OrderBy(e => e.Sequence)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            string previousHash = GenesisHash;
            foreach (DppAuditEntry entry in entries)
            {
                // Verify with the key that signed THIS entry, not whichever key happens to be
                // configured now. Recomputing history with the current key would make enabling a key
                // for the first time, or rotating one, look like wholesale tampering - a false alarm
                // on a security control, which is worse than no alarm because it teaches operators to
                // ignore it.
                DppAuditKeyLookup lookup = await _keyProvider.TryGetKeyByIdAsync(entry.KeyId, cancellationToken).ConfigureAwait(false);
                if (!lookup.Found)
                {
                    // Not tampering: the entry's key simply is not configured, so nothing can be said
                    // about it either way. Reporting this as tampering would be a lie; reporting it as
                    // valid would be worse.
                    _logger.LogError(
                        "DPP audit entry {Sequence} was signed with key {KeyId}, which is not configured. Add it to {RetiredKeysPath} to keep this entry verifiable.",
                        entry.Sequence,
                        entry.KeyId,
                        DppAuditKeyProvider.RetiredKeysConfigurationPath);
                    throw new DppAuditException(
                        $"Audit entry {entry.Sequence} was signed with key '{entry.KeyId}', which is not configured. " +
                        $"Its integrity cannot be determined. Configure the key under '{DppAuditKeyProvider.RetiredKeysConfigurationPath}' to verify it.");
                }

                if (entry.PreviousHash != previousHash || entry.EntryHash != ComputeHash(entry, previousHash, lookup.Key))
                {
                    return false;
                }

                previousHash = entry.EntryHash;
            }

            DppAuditCheckpoint checkpoint = await _db.DppAuditCheckpoints
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == DppAuditCheckpoint.SingletonId, cancellationToken)
                .ConfigureAwait(false);

            if (checkpoint is null)
            {
                // No checkpoint yet: only an empty log is consistent with never having appended.
                if (entries.Count == 0)
                {
                    return true;
                }

                _logger.LogError("DPP audit checkpoint is missing while {Count} entries exist; cannot rule out truncation.", entries.Count);
                return false;
            }

            if (checkpoint.EntryCount != entries.Count)
            {
                _logger.LogError(
                    "DPP audit log length mismatch: checkpoint expects {Expected} entries but {Actual} are present.",
                    checkpoint.EntryCount,
                    entries.Count);
                return false;
            }

            string expectedTail = entries.Count == 0 ? GenesisHash : entries[^1].EntryHash;
            if (!string.Equals(checkpoint.TailHash, expectedTail, StringComparison.Ordinal))
            {
                _logger.LogError("DPP audit log tail hash does not match the checkpoint; the most recent entries may have been replaced.");
                return false;
            }

            // The two checks above only prove the log agrees with the checkpoint. They do not prove
            // the checkpoint itself was not rewritten: truncating the log to a valid prefix and
            // restating EntryCount/TailHash to match that prefix satisfies both. Authenticating the
            // checkpoint closes that path, because a rolled-back checkpoint cannot be re-MAC'd
            // without the audit key. As with entries, use the key the checkpoint was written with.
            DppAuditKeyLookup checkpointKey = await _keyProvider.TryGetKeyByIdAsync(checkpoint.KeyId, cancellationToken).ConfigureAwait(false);
            if (!checkpointKey.Found)
            {
                _logger.LogError(
                    "DPP audit checkpoint was written with key {KeyId}, which is not configured; its integrity cannot be determined.",
                    checkpoint.KeyId);
                throw new DppAuditException(
                    $"The audit checkpoint was written with key '{checkpoint.KeyId}', which is not configured. " +
                    $"Configure it under '{DppAuditKeyProvider.RetiredKeysConfigurationPath}' to verify the checkpoint.");
            }

            // A null KeyId means "written while unkeyed", which resolves successfully by design so
            // pre-keying history still verifies. But the checkpoint lives in the database it protects,
            // so an attacker who can edit it could simply null KeyId and CheckpointMac and this branch
            // would skip MAC verification entirely - re-opening the truncate-to-valid-prefix attack
            // even though a key is configured. Once keyed auditing is on, an unkeyed checkpoint is
            // therefore refused rather than trusted.
            DppAuditKey currentKey = await _keyProvider.GetCurrentKeyAsync(cancellationToken).ConfigureAwait(false);
            if (currentKey.Key is not null && checkpointKey.Key is null)
            {
                // The previous heuristic accepted an unkeyed checkpoint whenever no entry carried a
                // KeyId. That inference reads the same attacker-writable columns the MAC exists to
                // defend: clearing every entry's KeyId along with the checkpoint's key and MAC
                // reproduces the "legacy" shape exactly, so it cannot distinguish genuine pre-key
                // history from a rewritten chain. Acceptance is therefore an explicit operator
                // decision, not something derived from the data under attack.
                if (!_allowUnkeyedCheckpointMigration)
                {
                    if (_logger.IsEnabled(LogLevel.Critical))
                    {
                        _logger.LogCritical(
                            "DPP audit checkpoint is unkeyed while keyed auditing is enabled. This is either an un-migrated log or a downgrade to bypass MAC verification; set {ConfigPath} to true only if this deployment is knowingly enabling a key over an existing unkeyed log.",
                            AllowUnkeyedCheckpointMigrationPath);
                    }

                    return false;
                }

                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(
                        "DPP audit checkpoint is unkeyed and accepted because {ConfigPath} is enabled. The pre-key entries are unauthenticated and their truncation is not detectable; disable the setting once the checkpoint has been re-keyed.",
                        AllowUnkeyedCheckpointMigrationPath);
                }

                return true;
            }

            if (checkpointKey.Key is not null)
            {
                string expectedMac = ComputeCheckpointMac(checkpoint.EntryCount, checkpoint.TailHash, checkpointKey.Key, checkpoint.KeyId);
                if (string.IsNullOrEmpty(checkpoint.CheckpointMac)
                    || !CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(checkpoint.CheckpointMac),
                        Encoding.UTF8.GetBytes(expectedMac)))
                {
                    _logger.LogError("DPP audit checkpoint MAC does not verify; the checkpoint has been altered or rolled back.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Authenticates the checkpoint's length, tail and key identifier. Returns null when no audit
        /// key is configured, in which case the checkpoint stays unauthenticated and rollback to an
        /// earlier valid prefix remains undetectable - see the audit-log limitations in the README.
        /// </summary>
        /// <remarks>
        /// The key id is covered by the MAC, not merely stored beside it: leaving it outside would let
        /// an attacker repoint a checkpoint at a different (say, leaked or retired) key while keeping
        /// the MAC intact.
        /// </remarks>
        internal static string ComputeCheckpointMac(long entryCount, string tailHash, byte[] key, string keyId = null)
        {
            if (key is null)
            {
                return null;
            }

            // Length-prefixed for the same reason entry hashing is: a delimiter could otherwise be
            // shifted between the count and the tail to produce a colliding input.
            using var buffer = new MemoryStream();
            AppendField(buffer, entryCount.ToString(CultureInfo.InvariantCulture));
            AppendField(buffer, tailHash);
            AppendField(buffer, keyId);

            using var hmac = new HMACSHA256(key);
            return Convert.ToHexString(hmac.ComputeHash(buffer.ToArray()));
        }

        /// <summary>
        /// Builds the canonical byte sequence an entry's hash covers. Fields are length-prefixed
        /// rather than delimiter-joined: a plain separator is ambiguous, because a caller-controlled
        /// field may contain the separator itself. With <c>|</c> joining, DppId "a|b" + ElementPath
        /// "c" and DppId "a" + ElementPath "b|c" produce the identical string, so an entry could be
        /// altered while keeping its hash and still passing chain verification. Prefixing each field
        /// with its byte length makes the encoding injective, so distinct field sets cannot collide.
        /// </summary>
        /// <summary>
        /// Drops sub-microsecond ticks so the value hashed here is byte-identical to the value
        /// PostgreSQL stores and returns. Without this the audit chain verifies only until the first
        /// reload, because the recomputed hash would cover a timestamp the database rounded away.
        /// </summary>
        internal static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
        {
            long ticksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;
            return new DateTimeOffset(value.Ticks - (value.Ticks % ticksPerMicrosecond), value.Offset);
        }

        /// <summary>
        /// Builds the digest an entry carries. Fields are length-prefixed rather than
        /// delimiter-joined, so caller-controlled values containing the separator cannot produce two
        /// different entries with the same digest.
        /// </summary>
        /// <param name="key">
        /// When supplied, HMAC-SHA256 is used instead of a bare SHA-256. This is what makes the log
        /// tamper-evident against someone who can write to the database: an unkeyed digest is a
        /// public function of the stored rows, so it can simply be recomputed after an edit. When
        /// null the log degrades to an integrity check only - see <see cref="DppAuditKeyProvider"/>.
        /// </param>
        internal static string ComputeHash(DppAuditEntry entry, string previousHash, byte[] key = null)
        {
            using var buffer = new MemoryStream();

            AppendField(buffer, previousHash);
            AppendField(buffer, entry.Timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            AppendField(buffer, entry.OperatorId);
            AppendField(buffer, entry.Operation.ToString());
            AppendField(buffer, entry.DppId);
            AppendField(buffer, entry.ElementPath);
            AppendField(buffer, entry.Outcome);
            AppendField(buffer, entry.OperationId);

            byte[] canonical = buffer.ToArray();

            if (key is { Length: > 0 })
            {
                using var hmac = new HMACSHA256(key);
                return Convert.ToHexString(hmac.ComputeHash(canonical));
            }

            return Convert.ToHexString(SHA256.HashData(canonical));
        }

        // Writes "<byte length>:<utf8 bytes>" so the field boundaries are recoverable from the
        // encoding alone. A null field is distinguished from an empty one by a length of -1.
        private static void AppendField(MemoryStream destination, string value)
        {
            if (value is null)
            {
                byte[] nullMarker = Encoding.UTF8.GetBytes("-1:");
                destination.Write(nullMarker, 0, nullMarker.Length);
                return;
            }

            byte[] payload = Encoding.UTF8.GetBytes(value);
            byte[] prefix = Encoding.UTF8.GetBytes(payload.Length.ToString(CultureInfo.InvariantCulture) + ":");
            destination.Write(prefix, 0, prefix.Length);
            destination.Write(payload, 0, payload.Length);
        }
    }
}
