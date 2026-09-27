using System;
using System.Globalization;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Strict ISO 8601 parser used by EN 18222 endpoints whose route segments must be unambiguous
    /// timestamps (currently <c>ReadDPPVersionByIdAndDate</c>).
    /// </summary>
    /// <remarks>
    /// The implementation deliberately uses <see cref="DateTimeOffset.TryParseExact(string, string[], IFormatProvider, DateTimeStyles, out DateTimeOffset)"/>
    /// rather than the permissive <see cref="DateTimeOffset.TryParse(string, IFormatProvider, DateTimeStyles, out DateTimeOffset)"/>:
    /// the latter silently accepts many culture-shaped strings under <see cref="CultureInfo.InvariantCulture"/>
    /// (for example <c>12/31/2024</c>, <c>31-Dec-2024</c>, <c>2024/12/31 14:30</c>) that the
    /// controller's error message explicitly rules out, letting clients submit ambiguous input
    /// and silently get the wrong snapshot back.
    /// <para>
    /// Lives outside the controller so the format set and parser behavior can be unit-tested
    /// directly. The controller is the only intended caller and is responsible for mapping the
    /// boolean result into the spec-shaped <see cref="Opc.Ua.Cloud.Library.Models.ApiResponse{T}"/>
    /// envelope.
    /// </para>
    /// </remarks>
    public static class DppDateParser
    {
        // The accepted format set is the explicit ISO 8601 grammar the contract documents:
        // calendar date with optional time (seconds, optional fractional seconds 1..7 digits)
        // and an optional zone designator ('Z' or +/-HH:mm). Millisecond/nanosecond precision is
        // accepted so the parser round-trips the PublicationDate stamps written by
        // DPPService.AdvancePublicationDate (millisecond precision, 'Z' suffix).
        private static readonly string[] s_iso8601Formats =
        {
            // Date only.
            "yyyy-MM-dd",

            // Date + time, no zone (treated as UTC via AssumeUniversal below).
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.f",
            "yyyy-MM-ddTHH:mm:ss.ff",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss.ffff",
            "yyyy-MM-ddTHH:mm:ss.fffff",
            "yyyy-MM-ddTHH:mm:ss.ffffff",
            "yyyy-MM-ddTHH:mm:ss.fffffff",

            // Date + time + 'Z' (explicit UTC).
            "yyyy-MM-ddTHH:mmZ",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fZ",
            "yyyy-MM-ddTHH:mm:ss.ffZ",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy-MM-ddTHH:mm:ss.ffffZ",
            "yyyy-MM-ddTHH:mm:ss.fffffZ",
            "yyyy-MM-ddTHH:mm:ss.ffffffZ",
            "yyyy-MM-ddTHH:mm:ss.fffffffZ",

            // Date + time + numeric offset (+HH:mm / -HH:mm).
            "yyyy-MM-ddTHH:mmzzz",
            "yyyy-MM-ddTHH:mm:sszzz",
            "yyyy-MM-ddTHH:mm:ss.fzzz",
            "yyyy-MM-ddTHH:mm:ss.ffzzz",
            "yyyy-MM-ddTHH:mm:ss.fffzzz",
            "yyyy-MM-ddTHH:mm:ss.ffffzzz",
            "yyyy-MM-ddTHH:mm:ss.fffffzzz",
            "yyyy-MM-ddTHH:mm:ss.ffffffzzz",
            "yyyy-MM-ddTHH:mm:ss.fffffffzzz",
        };

        /// <summary>
        /// Attempts to parse <paramref name="value"/> as an ISO 8601 timestamp using the documented
        /// grammar.
        /// </summary>
        /// <param name="value">The string to parse. <c>null</c>, empty, and whitespace input are rejected.</param>
        /// <param name="result">The parsed value, normalized to UTC. Set to <see cref="DateTimeOffset.MinValue"/> on failure.</param>
        /// <returns><c>true</c> when <paramref name="value"/> matches one of the accepted ISO 8601 forms.</returns>
        public static bool TryParse(string value, out DateTimeOffset result)
        {
            // string.IsNullOrWhiteSpace covers null, empty and all-whitespace input in one check.
            // TryParseExact would also reject these but the call would still allocate; short-circuit.
            if (string.IsNullOrWhiteSpace(value))
            {
                result = default;
                return false;
            }

            return DateTimeOffset.TryParseExact(
                value,
                s_iso8601Formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out result);
        }
    }
}
