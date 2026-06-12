using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        // Row-name layout: "dpp-archive::{dppId}::{capturedAtUtcTicks:D19}"
        // The fixed-width D19 tick stamp makes lexicographic ordering match chronological order,
        // which lets us pick the snapshot at-or-before a target time with an ordered prefix scan.
        private const string KeyPrefix = "dpp-archive::";
        private const string KeySeparator = "::";
        private const string TickFormat = "D19";

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

            // Tie-break by nudging forward so concurrent updates with identical timestamps don't collide
            // on the row primary key.
            string name = BuildRowName(dppId, ticks);
            while (await _storage.FindFileAsync(name).ConfigureAwait(false) != null)
            {
                ticks++;
                name = BuildRowName(dppId, ticks);
            }

            string payload = JsonConvert.SerializeObject(snapshot);
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
                return JsonConvert.DeserializeObject<DigitalProductPassport>(row.Blob);
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
            return BuildRowPrefix(dppId) + ticks.ToString(TickFormat, CultureInfo.InvariantCulture);
        }

        private static bool TryParseTicks(string rowName, string prefix, out long ticks)
        {
            ticks = 0;
            if (rowName == null || !rowName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            string tickField = rowName.Substring(prefix.Length);
            return long.TryParse(tickField, NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks);
        }
    }
}
