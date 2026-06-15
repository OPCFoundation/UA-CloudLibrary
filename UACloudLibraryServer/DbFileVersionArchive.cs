using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Durable <see cref="IDppVersionArchive"/> that stores each snapshot as a row in the
    /// existing <see cref="DbFileStorage"/>. Snapshots are serialized to JSON in the
    /// <see cref="DbFiles.Blob"/> column, and the row <see cref="DbFiles.Name"/> encodes
    /// both the DPP id and the capture timestamp so multiple versions can coexist without
    /// overwriting one another.
    /// </summary>
    public sealed class DbFileVersionArchive : IDppVersionArchive
    {
        // Row-name layout: "dpp-archive::{dppId}::{capturedAtUtcTicks:D19}-{counter:D6}{randomHex}"
        //
        // The fixed-width D19 tick stamp is the dominant sort key, so lexicographic ordering
        // still matches chronological order and the at-or-before lookup in GetVersionAtAsync
        // remains an ordered prefix scan. The "-{counter:D6}{randomHex}" suffix exists only to
        // break ties when multiple snapshots are captured at the same tick:
        //  - {counter:D6} is a per-process monotonic counter (Interlocked.Increment), so two
        //    in-process callers that observe the same tick are guaranteed to produce distinct,
        //    deterministically-ordered names without any DB round-trip.
        //  - {randomHex} is 8 hex chars from RandomNumberGenerator, making the row name unique
        //    across processes as well. This makes the underlying insert itself the arbiter:
        //    DbFiles.Name is the [Key] of the table, so the database uniqueness constraint
        //    rejects any genuine duplicate instead of the previous racy check-then-write loop
        //    (two callers can observe the same free name and both upsert through
        //    UploadFileAsync, which silently overwrites the loser's row).
        //
        // Within the same tick, sort order is (counter asc, randomHex asc); the at-or-before
        // query in GetVersionAtAsync still selects the greatest candidate whose ticks <= target,
        // which is the most recent write at that tick - the same behavior as before.
        private const string KeyPrefix = "dpp-archive::";
        private const string KeySeparator = "::";
        private const string TickFormat = "D19";
        private const char TickSuffixSeparator = '-';

        // Per-process monotonic counter for the in-process tie-break component of the row name.
        // Starts negative so the first increment wraps to a small positive value; the field is
        // long-sized so practical wraparound is impossible.
        private static long s_sequence;

        // Snapshots must round-trip the polymorphic DataElement hierarchy declared on
        // DigitalProductPassport. The model uses System.Text.Json polymorphism
        // ([JsonPolymorphic]/[JsonDerivedType] with discriminator "objectType") plus
        // [JsonPropertyName] overrides and [JsonStringEnumConverter] on Granularity, so the
        // archive must (de)serialize with System.Text.Json - Newtonsoft.Json would ignore
        // those attributes and fail to instantiate the abstract DataElement base type on read.
        // DefaultIgnoreCondition.WhenWritingNull keeps optional members (e.g. FacilityId,
        // ContentSpecificationIds, DictionaryReference, ValueDataType) out of the stored blob.
        private static readonly JsonSerializerOptions s_snapshotJsonOptions = new() {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        private readonly DbFileStorage _storage;
        private readonly ILogger<DbFileVersionArchive> _logger;

        public DbFileVersionArchive(DbFileStorage storage, ILogger<DbFileVersionArchive> logger)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ArchiveAsync(string dppId, DigitalProductPassport snapshot, DateTimeOffset capturedAtUtc)
        {
            if (string.IsNullOrEmpty(dppId))
            {
                throw new ArgumentException("dppId must not be empty.", nameof(dppId));
            }

            ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));

            long ticks = capturedAtUtc.ToUniversalTime().UtcTicks;

            // The row name carries a per-process counter plus a random tail, so it is unique
            // by construction and the underlying DbFiles primary key (DbFiles.Name) is the
            // sole arbiter of collisions. No check-then-write loop is needed.
            string name = BuildRowName(dppId, ticks);

            string payload = JsonSerializer.Serialize(snapshot, s_snapshotJsonOptions);
            string stored = await _storage.UploadFileAsync(name, payload, null).ConfigureAwait(false);
            if (string.IsNullOrEmpty(stored))
            {
                _logger.LogError("DbFileVersionArchive: failed to persist snapshot for DPP {DppId} at {CapturedAtUtc}.", dppId, capturedAtUtc);
            }
        }

        public async Task<DigitalProductPassport> GetVersionAtAsync(string dppId, DateTimeOffset asOfUtc)
        {
            if (string.IsNullOrEmpty(dppId))
            {
                return null;
            }

            string prefix = BuildRowPrefix(dppId);
            IReadOnlyList<string> names = await _storage.ListFileNamesAsync(prefix).ConfigureAwait(false);
            if (names == null || names.Count == 0)
            {
                return null;
            }

            long targetTicks = asOfUtc.ToUniversalTime().UtcTicks;
            string match = null;

            // Names are returned in ascending order; pick the greatest one whose ticks <= target.
            // Within the same tick, the (counter, randomHex) suffix orders ties deterministically,
            // so this still selects the most recent write at that tick.
            foreach (string candidate in names)
            {
                if (!TryParseTicks(candidate, prefix, out long candidateTicks))
                {
                    continue;
                }

                if (candidateTicks > targetTicks)
                {
                    break;
                }

                match = candidate;
            }

            if (match == null)
            {
                return null;
            }

            DbFiles row = await _storage.DownloadFileAsync(match).ConfigureAwait(false);
            if (row == null || string.IsNullOrEmpty(row.Blob))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<DigitalProductPassport>(row.Blob, s_snapshotJsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "DbFileVersionArchive: failed to deserialize snapshot row {Name}.", match);
                return null;
            }
        }

        private static string BuildRowPrefix(string dppId)
        {
            return KeyPrefix + dppId + KeySeparator;
        }

        private static string BuildRowName(string dppId, long ticks)
        {
            // Combine the per-process counter with a short random tail so the resulting name is
            // unique across both threads and processes, eliminating the previous check-then-write
            // race. The "-" separator is reserved (D19 ticks are all digits) so TryParseTicks can
            // recover the tick field by splitting on the first '-'.
            long sequence = Interlocked.Increment(ref s_sequence) & 0xFFFFFF; // 6 hex digits' worth
            string randomHex = NextRandomHex8();
            return BuildRowPrefix(dppId)
                + ticks.ToString(TickFormat, CultureInfo.InvariantCulture)
                + TickSuffixSeparator
                + sequence.ToString("D6", CultureInfo.InvariantCulture)
                + randomHex;
        }

        private static string NextRandomHex8()
        {
            Span<byte> bytes = stackalloc byte[4];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes);
        }

        private static bool TryParseTicks(string rowName, string prefix, out long ticks)
        {
            ticks = 0;
            if (rowName == null || !rowName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            string tail = rowName.Substring(prefix.Length);

            // The tail is "{ticks:D19}-{counter:D6}{randomHex}"; isolate the tick field by
            // splitting on the first '-'. For backward compatibility, also accept the legacy
            // "{ticks:D19}" layout (no suffix) so any previously archived rows still parse.
            int separatorIndex = tail.IndexOf(TickSuffixSeparator, StringComparison.Ordinal);
            string tickField = separatorIndex < 0 ? tail : tail.Substring(0, separatorIndex);

            return long.TryParse(tickField, NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks);
        }
    }
}
