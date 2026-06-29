using System;
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
    /// EF-backed, hash-chained audit log. Appends are serialized through a process-wide semaphore so
    /// the chain stays linear, and each entry's hash covers its content plus the previous hash, making
    /// the log tamper-evident (EN 18246 §4.7).
    /// </summary>
    public class DppAuditLog : IDppAuditLog
    {
        private const string GenesisHash = "0000000000000000000000000000000000000000000000000000000000000000";
        private static readonly SemaphoreSlim s_appendLock = new(1, 1);

        private readonly AppDbContext _db;
        private readonly ILogger _logger;

        public DppAuditLog(AppDbContext db, ILoggerFactory loggerFactory)
        {
            _db = db;
            _logger = loggerFactory.CreateLogger("DppAuditLog");
        }

        public async Task RecordAsync(string operatorId, DppAuditOperation operation, string dppId, string elementPath, string outcome)
        {
            await s_appendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                DppAuditEntry tail = await _db.DppAuditEntries.OrderByDescending(e => e.Sequence).FirstOrDefaultAsync().ConfigureAwait(false);
                string previousHash = tail?.EntryHash ?? GenesisHash;

                var entry = new DppAuditEntry
                {
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
                await _db.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Auditing must never break the request path; record the failure for operators.
                _logger.LogError(ex, "Failed to append DPP audit entry for {Operation} on {DppId}", operation, dppId);
            }
            finally
            {
                s_appendLock.Release();
            }
        }

        public async Task<bool> VerifyChainAsync()
        {
            string previousHash = GenesisHash;
            foreach (DppAuditEntry entry in await _db.DppAuditEntries.OrderBy(e => e.Sequence).ToListAsync().ConfigureAwait(false))
            {
                if (entry.PreviousHash != previousHash || entry.EntryHash != ComputeHash(entry, previousHash))
                {
                    return false;
                }

                previousHash = entry.EntryHash;
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
