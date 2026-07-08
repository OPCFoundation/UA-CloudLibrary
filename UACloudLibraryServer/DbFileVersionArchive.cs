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
        // Row-name layout: "dpp-archive::{dppId}::{capturedAtUtcTicks:D19}-{counter:X6}{randomHex}"
        //
        // The fixed-width D19 tick stamp is the dominant sort key, so lexicographic ordering
        // still matches chronological order and the at-or-before lookup in GetVersionAtAsync
        // remains an ordered prefix scan. The "-{counter:X6}{randomHex}" suffix exists only to
        // break ties when multiple snapshots are captured at the same tick:
        //  - {counter:X6} is a per-process monotonic counter (Interlocked.Increment) masked to
        //    24 bits and formatted as 6 uppercase hex chars so the field stays exactly 6 chars
        //    wide (decimal D6 could exceed 6 chars once the counter rolled past 999,999 and
        //    would break the lexicographic-equals-chronological ordering within a single tick).
        //    Two in-process callers that observe the same tick are guaranteed to produce
        //    distinct, deterministically-ordered names without any DB round-trip.
        //  - {randomHex} is 8 hex chars from RandomNumberGenerator, extending the tie-break
        //    across processes as well. Combined with the per-process counter, the name is
        //    probabilistically unique by construction, which removes the previous racy
        //    check-then-write loop entirely. Note that the underlying writer
        //    (DbFileStorage.UploadFileAsync) is an upsert keyed on DbFiles.Name, so the
        //    DbFiles primary key does not actively reject a duplicate - it silently overwrites
        //    the prior row instead. Collisions are therefore not impossible, only vanishingly
        //    unlikely (~1 in 2^32 per same-tick same-counter pair). Switching to an insert-only
        //    write path would turn that into a hard rejection, which is a deliberate
        //    open-ended improvement: today the archive trades a negligible collision risk for
        //    a single, lock-free write per snapshot.
        //
        // Within the same tick, sort order is (counter asc, randomHex asc); the at-or-before
        // query in GetVersionAtAsync still selects the greatest candidate whose ticks <= target,
        // which is the most recent write at that tick - the same behavior as before.
        private const string KeyPrefix = "dpp-archive::";
        private const string KeySeparator = "::";
        private const string TickFormat = "D19";
        private const char TickSuffixSeparator = '-';

        // Per-process monotonic counter for the in-process tie-break component of the row name.
        // Initialized to the default 0; the first Interlocked.Increment yields 1, then increments
        // climb monotonically. BuildRowName masks the result with 0xFFFFFF so the field always
        // fits in the fixed 6-char X6 hex slot, which means wraparound is harmless and the long
        // backing field is effectively never exhausted in practice.
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

        public async Task<bool> ArchiveAsync(string dppId, DigitalProductPassport snapshot, DateTimeOffset capturedAtUtc)
        {
            if (string.IsNullOrEmpty(dppId))
            {
                throw new ArgumentException("dppId must not be empty.", nameof(dppId));
            }

            ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));

            long ticks = capturedAtUtc.ToUniversalTime().UtcTicks;

            // The row name carries a per-process counter plus a random tail, so it is
            // probabilistically unique by construction. That removes the need for a
            // check-then-write loop: the only remaining failure mode is an astronomically
            // unlikely (counter, randomHex) collision within the same tick, in which case
            // DbFileStorage.UploadFileAsync (an upsert) would silently overwrite the prior
            // row. The collision odds (~1 in 2^32 per same-tick same-counter pair) are well
            // below the practical concern threshold for an archive workload.
            string name = BuildRowName(dppId, ticks);

            string payload = JsonSerializer.Serialize(snapshot, s_snapshotJsonOptions);
            string stored = await _storage.UploadFileAsync(name, payload, null).ConfigureAwait(false);
            if (string.IsNullOrEmpty(stored))
            {
                // Per EN 18221 Clause 4.2 the archive write is part of the update transaction:
                // surfacing the failure lets DPPService translate it into WriteFailed so the
                // client sees the broken archival guarantee instead of a false-positive 200.
                _logger.LogError("DbFileVersionArchive: failed to persist snapshot for DPP {DppId} at {CapturedAtUtc}.", dppId, capturedAtUtc);
                return false;
            }

            return true;
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
            // probabilistically unique across both threads and processes, eliminating the previous
            // check-then-write race. The "-" separator is reserved (D19 ticks are all digits) so
            // TryParseTicks can recover the tick field by splitting on the first '-'. The counter
            // is formatted as a 6-char uppercase hex value so the 24-bit mask matches the field
            // width exactly -- keeping the suffix fixed-width preserves lexicographic =
            // chronological ordering for collisions at the same tick.
            long sequence = Interlocked.Increment(ref s_sequence) & 0xFFFFFF; // 24 bits = 6 hex digits
            string randomHex = NextRandomHex8();
            return BuildRowPrefix(dppId)
                + ticks.ToString(TickFormat, CultureInfo.InvariantCulture)
                + TickSuffixSeparator
                + sequence.ToString("X6", CultureInfo.InvariantCulture)
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

            // The tail is "{ticks:D19}-{counter:X6}{randomHex}"; isolate the tick field by
            // splitting on the first '-'. For backward compatibility, also accept the legacy
            // "{ticks:D19}" layout (no suffix) so any previously archived rows still parse.
            int separatorIndex = tail.IndexOf(TickSuffixSeparator, StringComparison.Ordinal);
            string tickField = separatorIndex < 0 ? tail : tail.Substring(0, separatorIndex);

            return long.TryParse(tickField, NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks);
        }
    }
}
