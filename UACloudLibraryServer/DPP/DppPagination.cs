using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Pure pagination algorithm used by EN 18222 <c>ReadDPPIdsByProductIds</c>.
    /// </summary>
    /// <remarks>
    /// Lives outside the controller so the cursor / dedup / sort / page-slice behavior can be
    /// exercised by unit tests without spinning up the ASP.NET Core pipeline. The controller is
    /// the only intended caller and is responsible for mapping <see cref="SliceOutcome"/> into the
    /// existing <see cref="ApiResponse{T}"/> envelopes.
    /// </remarks>
    public static class DppPagination
    {
        /// <summary>
        /// Result of <see cref="TrySlice"/>.
        /// </summary>
        public enum SliceOutcome
        {
            /// <summary>The cursor parsed and the page was produced (possibly empty).</summary>
            Success,

            /// <summary>The supplied cursor was not a non-negative invariant-culture integer.</summary>
            CursorMalformed,
        }

        /// <summary>
        /// Applies the spec-required slicing rule to <paramref name="rawIds"/>.
        /// </summary>
        /// <remarks>
        /// The input list comes from <see cref="DPPService.GetDppIdsByProductIds"/>, which does
        /// NOT guarantee a deterministic order and CAN contain duplicates when the same DPP backs
        /// multiple productIds. The integer cursor only produces stable pages if the underlying
        /// sequence is itself stable, so this helper:
        /// <list type="number">
        ///   <item>drops null / empty ids,</item>
        ///   <item>deduplicates with <see cref="StringComparer.Ordinal"/>,</item>
        ///   <item>sorts ordinal-ascending so successive calls position the same cursor at the
        ///   same id,</item>
        ///   <item>parses the cursor (clamping to the end of the sequence when it overshoots,
        ///   so an over-large cursor returns an empty terminal page rather than a 400), and</item>
        ///   <item>emits <c>nextCursor</c> + <c>hasMore</c> only when <paramref name="limit"/>
        ///   actually cut the page short.</item>
        /// </list>
        /// The <see cref="Pagination"/> envelope is returned only when the caller asked for paging
        /// (either <paramref name="limit"/> or <paramref name="cursor"/> was supplied); non-paged
        /// callers get a <c>null</c> envelope so the response object omits it on the wire.
        /// </remarks>
        public static SliceOutcome TrySlice(
            IEnumerable<string> rawIds,
            int? limit,
            string cursor,
            out List<string> page,
            out Pagination pagination,
            out string errorMessage)
        {
            page = null;
            pagination = null;
            errorMessage = null;

            ArgumentNullException.ThrowIfNull(rawIds);

            List<string> ids = rawIds
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();

            int startIndex = 0;
            if (!string.IsNullOrEmpty(cursor))
            {
                // A cursor was supplied: it must be a valid non-negative invariant-culture
                // integer (the same format we emit in 'nextCursor'). Silently treating a
                // malformed cursor as "start over" makes pagination loop on the client.
                if (!int.TryParse(cursor, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedCursor)
                    || parsedCursor < 0)
                {
                    errorMessage = "cursor must be a non-negative integer";
                    return SliceOutcome.CursorMalformed;
                }

                // Clamp to the end of the sequence so an over-large cursor returns an empty
                // terminal page rather than throwing or producing a bogus negative slice.
                startIndex = Math.Min(parsedCursor, ids.Count);
            }

            List<string> sliced = ids.Skip(startIndex).ToList();
            string nextCursor = null;
            bool hasMore = false;

            if (limit is int max && sliced.Count > max)
            {
                sliced = sliced.Take(max).ToList();
                int next = startIndex + max;
                nextCursor = next.ToString(CultureInfo.InvariantCulture);
                hasMore = true;
            }

            page = sliced;

            // EN 18222 keeps pagination metadata adjacent to, but separate from, the payload.
            // Only emit the envelope when the caller actually asked for paging, otherwise the
            // response should omit "pagination" entirely.
            if (limit.HasValue || !string.IsNullOrEmpty(cursor))
            {
                pagination = new Pagination(nextCursor, hasMore, limit);
            }

            return SliceOutcome.Success;
        }
    }
}
