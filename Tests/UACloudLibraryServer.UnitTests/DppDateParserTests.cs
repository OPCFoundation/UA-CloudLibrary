using System;
using System.Globalization;

using Opc.Ua.Cloud.Library;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="DppDateParser.TryParse"/>. The strict ISO 8601 parser backs the
    /// EN 18222 <c>ReadDPPVersionByIdAndDate</c> endpoint, where ambiguous input would silently
    /// return the wrong snapshot. These tests pin:
    /// <list type="bullet">
    ///   <item>the documented accepted forms (date only, date+time, fractional seconds, 'Z',
    ///   numeric offset),</item>
    ///   <item>UTC normalization regardless of the input zone,</item>
    ///   <item>rejection of the culture-shaped strings called out in the controller comment
    ///   (<c>12/31/2024</c>, <c>31-Dec-2024</c>, <c>2024/12/31 14:30</c>) that
    ///   <see cref="DateTimeOffset.TryParse(string, IFormatProvider, DateTimeStyles, out DateTimeOffset)"/>
    ///   would otherwise accept,</item>
    ///   <item>round-trip with the millisecond-precision stamp emitted by
    ///   <c>DPPService.AdvancePublicationDate</c>.</item>
    /// </list>
    /// </summary>
    public class DppDateParserTests
    {
        [Theory]
        [InlineData("2024-12-31")]
        [InlineData("2024-12-31T14:30")]
        [InlineData("2024-12-31T14:30:45")]
        [InlineData("2024-12-31T14:30:45Z")]
        [InlineData("2024-12-31T14:30:45.123")]
        [InlineData("2024-12-31T14:30:45.123Z")]
        [InlineData("2024-12-31T14:30:45.1234567Z")]
        [InlineData("2024-12-31T14:30:45+02:00")]
        [InlineData("2024-12-31T14:30:45.123+02:00")]
        [InlineData("2024-12-31T14:30:45.1234567-05:30")]
        public void TryParse_AcceptsDocumentedIso8601Forms(string value)
        {
            Assert.True(DppDateParser.TryParse(value, out DateTimeOffset result),
                $"Expected '{value}' to parse as ISO 8601.");
            Assert.Equal(TimeSpan.Zero, result.Offset); // AdjustToUniversal normalizes the offset.
        }

        [Fact]
        public void TryParse_NormalizesPositiveOffsetToUtc()
        {
            // 2024-12-31T14:30:45+02:00 is 2024-12-31T12:30:45Z.
            Assert.True(DppDateParser.TryParse("2024-12-31T14:30:45+02:00", out DateTimeOffset result));
            Assert.Equal(new DateTimeOffset(2024, 12, 31, 12, 30, 45, TimeSpan.Zero), result);
        }

        [Fact]
        public void TryParse_NormalizesNegativeOffsetToUtc()
        {
            // 2024-12-31T14:30:45-05:30 is 2024-12-31T20:00:45Z.
            Assert.True(DppDateParser.TryParse("2024-12-31T14:30:45-05:30", out DateTimeOffset result));
            Assert.Equal(new DateTimeOffset(2024, 12, 31, 20, 0, 45, TimeSpan.Zero), result);
        }

        [Fact]
        public void TryParse_NoZoneDesignator_AssumesUtc()
        {
            // AssumeUniversal means an absent zone is treated as UTC rather than local time,
            // which is what the EN 18222 contract documents.
            Assert.True(DppDateParser.TryParse("2024-12-31T14:30:45", out DateTimeOffset result));
            Assert.Equal(new DateTimeOffset(2024, 12, 31, 14, 30, 45, TimeSpan.Zero), result);
        }

        [Fact]
        public void TryParse_DateOnly_ProducesMidnightUtc()
        {
            Assert.True(DppDateParser.TryParse("2024-12-31", out DateTimeOffset result));
            Assert.Equal(new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero), result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TryParse_NullOrWhitespace_IsRejected(string value)
        {
            Assert.False(DppDateParser.TryParse(value, out DateTimeOffset result));
            Assert.Equal(default, result);
        }

        [Theory]
        [InlineData("12/31/2024")]           // US slash date — silently accepted by TryParse
        [InlineData("31-Dec-2024")]          // mixed alpha/numeric form
        [InlineData("2024/12/31 14:30")]     // slash date + space-separator
        [InlineData("2024-12-31 14:30")]     // ISO date but space instead of 'T'
        [InlineData("December 31, 2024")]    // long form
        [InlineData("2024-13-01")]           // invalid month
        [InlineData("garbage")]
        public void TryParse_CultureShapedInput_IsRejected(string value)
        {
            // Each of these would slip past DateTimeOffset.TryParse(... InvariantCulture, ...) but
            // is outside the strict ISO 8601 grammar the contract documents.
            // Note: the RFC 3339 basic-offset form "+HHmm" (e.g. "2024-12-31T14:30:45+0200") is
            // explicitly accepted by the documented "zzz" format and is NOT rejected here.
            Assert.False(DppDateParser.TryParse(value, out _),
                $"Expected '{value}' to be rejected by the strict ISO 8601 parser.");
        }

        [Fact]
        public void TryParse_RoundTripsAdvancePublicationDateStamp()
        {
            // DPPService.AdvancePublicationDate writes UTC stamps as "yyyy-MM-ddTHH:mm:ss.fffZ".
            // The parser must accept its own output so persisted PublicationDate values can be
            // round-tripped through the versions endpoint without precision loss.
            var stamp = new DateTimeOffset(2025, 1, 15, 9, 30, 7, 123, TimeSpan.Zero);
            string formatted = stamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

            Assert.True(DppDateParser.TryParse(formatted, out DateTimeOffset parsed));
            Assert.Equal(stamp, parsed);
        }
    }
}
