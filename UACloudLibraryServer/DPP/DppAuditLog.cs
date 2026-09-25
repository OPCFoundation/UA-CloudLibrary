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

        private readonly AppDbContext _db;
        private readonly IDppAuditKeyProvider _keyProvider;
        private readonly ILogger _logger;

        public DppAuditLog(AppDbContext db, IDppAuditKeyProvider keyProvider, ILoggerFactory loggerFactory)
        {
            _db = db;
            _keyProvider = keyProvider;
            _logger = loggerFactory.CreateLogger("DppAuditLog");
        }

        /// <summary>
        /// Appends an audit entry, retrying on transient serialization conflicts. Throws
        /// <see cref="DppAuditException"/> when the entry cannot be durably committed: callers must
        /// fail the originating operation rather than proceed with an unlogged access or change,
        /// because silently dropping the record would forfeit the non-repudiation guarantee.
        /// </summary>
        public async Task RecordAsync(string operatorId, DppAuditOperation operation, string dppId, string elementPath, string outcome)
        {
            Exception lastError = null;

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                await s_appendLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    await AppendOnceAsync(operatorId, operation, dppId, elementPath, outcome).ConfigureAwait(false);
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

        private async Task AppendOnceAsync(string operatorId, DppAuditOperation operation, string dppId, string elementPath, string outcome)
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
                    long existing = await _db.DppAuditEntries.LongCountAsync().ConfigureAwait(false);
                    checkpoint = new DppAuditCheckpoint {
                        Id = DppAuditCheckpoint.SingletonId,
                        EntryCount = existing + 1
                    };
                    _db.DppAuditCheckpoints.Add(checkpoint);
                }
                else
                {
                    checkpoint.EntryCount += 1;
                }

                checkpoint.TailHash = entry.EntryHash;
                checkpoint.UpdatedAt = entry.Timestamp;
                checkpoint.CheckpointMac = ComputeCheckpointMac(checkpoint.EntryCount, checkpoint.TailHash, auditKey.Key);
                checkpoint.KeyId = auditKey.KeyId;

                await _db.SaveChangesAsync().ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Recomputes the hash chain and compares it against the persisted checkpoint. Returns false
        /// when any entry was altered or inserted, and also when entries were removed from the end -
        /// a case the chain alone cannot reveal, since a truncated log is still a valid prefix.
        /// </summary>
        public async Task<bool> VerifyChainAsync()
        {
            // AppDbContext enables EnableRetryOnFailure, and NpgsqlRetryingExecutionStrategy rejects
            // user-initiated transactions, so the snapshot below has to be opened by the strategy.
            IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(VerifyChainInSnapshotAsync).ConfigureAwait(false);
        }

        private async Task<bool> VerifyChainInSnapshotAsync()
        {
            // The entries and the checkpoint must come from the same snapshot. Read separately under
            // the default read-committed isolation, an append committing between the two queries would
            // pair the old entry list with the advanced checkpoint, and verification would report
            // tampering for a chain that is actually valid. RepeatableRead pins both reads to one
            // snapshot so a concurrent AppendOnceAsync cannot produce a spurious failure.
            await using var snapshot = await _db.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead)
                .ConfigureAwait(false);

            List<DppAuditEntry> entries = await _db.DppAuditEntries
                .AsNoTracking()
                .OrderBy(e => e.Sequence)
                .ToListAsync()
                .ConfigureAwait(false);

            string previousHash = GenesisHash;
            foreach (DppAuditEntry entry in entries)
            {
                // Verify with the key that signed THIS entry, not whichever key happens to be
                // configured now. Recomputing history with the current key would make enabling a key
                // for the first time, or rotating one, look like wholesale tampering - a false alarm
                // on a security control, which is worse than no alarm because it teaches operators to
                // ignore it.
                DppAuditKeyLookup lookup = await _keyProvider.TryGetKeyByIdAsync(entry.KeyId).ConfigureAwait(false);
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
                .FirstOrDefaultAsync(c => c.Id == DppAuditCheckpoint.SingletonId)
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
            DppAuditKeyLookup checkpointKey = await _keyProvider.TryGetKeyByIdAsync(checkpoint.KeyId).ConfigureAwait(false);
            if (!checkpointKey.Found)
            {
                _logger.LogError(
                    "DPP audit checkpoint was written with key {KeyId}, which is not configured; its integrity cannot be determined.",
                    checkpoint.KeyId);
                throw new DppAuditException(
                    $"The audit checkpoint was written with key '{checkpoint.KeyId}', which is not configured. " +
                    $"Configure it under '{DppAuditKeyProvider.RetiredKeysConfigurationPath}' to verify the checkpoint.");
            }

            if (checkpointKey.Key is not null)
            {
                string expectedMac = ComputeCheckpointMac(checkpoint.EntryCount, checkpoint.TailHash, checkpointKey.Key);
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
        /// Authenticates the checkpoint's length and tail. Returns null when no audit key is
        /// configured, in which case the checkpoint stays unauthenticated and rollback to an earlier
        /// valid prefix remains undetectable - see the audit-log limitations in the README.
        /// </summary>
        internal static string ComputeCheckpointMac(long entryCount, string tailHash, byte[] key)
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
