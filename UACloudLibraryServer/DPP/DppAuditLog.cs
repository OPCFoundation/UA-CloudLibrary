using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
        private readonly ILogger _logger;

        public DppAuditLog(AppDbContext db, ILoggerFactory loggerFactory)
        {
            _db = db;
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

        private void ResetTracking()
        {
            // Drop tracked state so the next attempt re-reads the current tail instead of replaying stale values.
            _db.ChangeTracker.Clear();
        }

        private async Task AppendOnceAsync(string operatorId, DppAuditOperation operation, string dppId, string elementPath, string outcome)
        {
            // Serializable isolation prevents two instances from reading the same tail and writing
            // entries that claim the same predecessor.
            using var transaction = await _db.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable)
                .ConfigureAwait(false);

            DppAuditEntry tail = await _db.DppAuditEntries
                .OrderByDescending(e => e.Sequence)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            string previousHash = tail?.EntryHash ?? GenesisHash;

            var entry = new DppAuditEntry {
                Timestamp = DateTimeOffset.UtcNow,
                OperatorId = string.IsNullOrEmpty(operatorId) ? "anonymous" : operatorId,
                Operation = operation,
                DppId = dppId,
                ElementPath = elementPath,
                Outcome = outcome,
                PreviousHash = previousHash
            };
            entry.EntryHash = ComputeHash(entry, previousHash);

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

            await _db.SaveChangesAsync().ConfigureAwait(false);
            await transaction.CommitAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Recomputes the hash chain and compares it against the persisted checkpoint. Returns false
        /// when any entry was altered or inserted, and also when entries were removed from the end -
        /// a case the chain alone cannot reveal, since a truncated log is still a valid prefix.
        /// </summary>
        public async Task<bool> VerifyChainAsync()
        {
            List<DppAuditEntry> entries = await _db.DppAuditEntries
                .OrderBy(e => e.Sequence)
                .ToListAsync()
                .ConfigureAwait(false);

            string previousHash = GenesisHash;
            foreach (DppAuditEntry entry in entries)
            {
                if (entry.PreviousHash != previousHash || entry.EntryHash != ComputeHash(entry, previousHash))
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

            return true;
        }

        private static string ComputeHash(DppAuditEntry entry, string previousHash)
        {
            string canonical = string.Join('|',
                previousHash,
                entry.Timestamp.ToUniversalTime().ToString("O"),
                entry.OperatorId,
                entry.Operation,
                entry.DppId,
                entry.ElementPath,
                entry.Outcome);

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
            return Convert.ToHexString(hash);
        }
    }
}
