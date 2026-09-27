using System;
using System.Collections.Generic;
using System.Text.Json;

using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Models;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Tests for the archived-snapshot blob format. A version snapshot has to carry the access
    /// policy that applied to it: the <c>controlledElements</c> mapping lives in the DPP's values
    /// blob, which only ever describes the DPP as it is now, so filtering history against it would
    /// expose an element that was controlled at the requested date and un-mapped since.
    /// </summary>
    public class DppVersionSnapshotTests
    {
        private static DigitalProductPassport SampleDpp() => new()
        {
            DigitalProductPassportId = "dpp-1",
            UniqueProductIdentifier = "prod-1",
            DppSchemaVersion = "1.0",
            DppStatus = "active",
            LastUpdate = DateTimeOffset.UtcNow,
            EconomicOperatorId = "EO-1",
            Elements = new List<DataElement>()
        };

        private const string PolicyJson = """
            { "controlledElements": { "weight": "Auditor" } }
            """;

        [Fact]
        public void Envelope_RoundTripsTheDppAndItsPolicy()
        {
            string blob = DbFileVersionArchive.WriteSnapshotBlob(SampleDpp(), PolicyJson);

            DppVersionSnapshot snapshot = DbFileVersionArchive.ReadSnapshotBlob(blob);

            Assert.NotNull(snapshot);
            Assert.True(snapshot.PolicyArchived);
            Assert.Equal("dpp-1", snapshot.Dpp.DigitalProductPassportId);

            DppControlledElements.MappingResult mapping =
                DppControlledElements.Read(snapshot.ControlledElementsValuesJson);

            Assert.Equal(DppControlledElements.MappingState.Valid, mapping.State);
            Assert.Equal("Auditor", Assert.Single(mapping.Entries["weight"]));
        }

        [Fact]
        public void Envelope_RoundTripsANullPolicyAsArchived()
        {
            // A DPP that genuinely had no values blob still has a *known* policy - "none" - which is
            // different from a snapshot that never recorded one. Only the latter may fail closed.
            string blob = DbFileVersionArchive.WriteSnapshotBlob(SampleDpp(), null);

            DppVersionSnapshot snapshot = DbFileVersionArchive.ReadSnapshotBlob(blob);

            Assert.True(snapshot.PolicyArchived);
            Assert.Equal(DppControlledElements.MappingState.Absent,
                DppControlledElements.Read(snapshot.ControlledElementsValuesJson).State);
        }

        [Fact]
        public void PreEnvelopeRow_StillDeserializes_ButReportsNoArchivedPolicy()
        {
            // Rows written before the archive recorded policy hold a bare serialized DPP. They must
            // keep working, but their policy is unknowable after the fact, so the caller has to be
            // told it was never archived rather than being handed a silently-empty mapping.
            string legacyBlob = JsonSerializer.Serialize(SampleDpp());

            DppVersionSnapshot snapshot = DbFileVersionArchive.ReadSnapshotBlob(legacyBlob);

            Assert.NotNull(snapshot);
            Assert.False(snapshot.PolicyArchived);
            Assert.Equal("dpp-1", snapshot.Dpp.DigitalProductPassportId);
            Assert.Null(snapshot.ControlledElementsValuesJson);
        }

        [Fact]
        public void MalformedBlob_Throws()
        {
            // JsonReaderException derives from JsonException, which is what the archive's read path
            // catches, so assert on the base type rather than the exact one.
            Assert.ThrowsAny<JsonException>(() => DbFileVersionArchive.ReadSnapshotBlob("{ not json"));
        }
    }
}
