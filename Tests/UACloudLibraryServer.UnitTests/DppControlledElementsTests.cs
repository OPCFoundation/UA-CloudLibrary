using System;

using Opc.Ua.Cloud.Library;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="DppControlledElements"/>: parsing the reserved <c>controlledElements</c>
    /// object out of a DPP values JSON (single role or roles array), and preserving it across a
    /// browse-and-persist value rewrite.
    /// </summary>
    public class DppControlledElementsTests
    {
        private static readonly string[] s_recyclerRepairer = { "Recycler", "Repairer" };
        private static readonly string[] s_customs = { "Customs" };
        private static readonly string[] s_recycler = { "Recycler" };

        [Fact]
        public void Parse_ReadsSingleRoleAndRolesArray()
        {
            const string values = """
            {
              "ns=1;i=1": "42",
              "controlledElements": {
                "materials.billOfMaterials": [ "Recycler", "Repairer" ],
                "supplierInfo": "Customs"
              }
            }
            """;

            DppControlledElements.MappingResult map = DppControlledElements.Read(values);

            Assert.Equal(DppControlledElements.MappingState.Valid, map.State);
            Assert.Equal(2, map.Entries.Count);
            Assert.Equal(s_recyclerRepairer, map.Entries["materials.billOfMaterials"]);
            Assert.Equal(s_customs, map.Entries["supplierInfo"]);
        }

        [Fact]
        public void Read_NoControlledElements_IsAbsentAndPublic()
        {
            Assert.Equal(DppControlledElements.MappingState.Absent, DppControlledElements.Read("{ \"ns=1;i=1\": \"42\" }").State);
            Assert.Equal(DppControlledElements.MappingState.Absent, DppControlledElements.Read(null).State);
        }

        [Fact]
        public void Read_MalformedValuesJson_IsInvalidRatherThanPublic()
        {
            // A parse failure must not be reported as "no controlled elements": callers treat an empty
            // mapping as "everything is public", which would expose controlled data on corruption.
            DppControlledElements.MappingResult result = DppControlledElements.Read("not json");

            Assert.Equal(DppControlledElements.MappingState.Invalid, result.State);
            Assert.True(result.IsInvalid);
        }

        [Fact]
        public void Read_MalformedMappingShapes_AreInvalid()
        {
            // controlledElements present but not an object.
            Assert.True(DppControlledElements.Read("{ \"controlledElements\": \"Recycler\" }").IsInvalid);

            // An entry naming no usable role would otherwise be silently dropped, publishing it.
            Assert.True(DppControlledElements.Read("{ \"controlledElements\": { \"materials\": [] } }").IsInvalid);
            Assert.True(DppControlledElements.Read("{ \"controlledElements\": { \"materials\": null } }").IsInvalid);

            // Root that is not a JSON object.
            Assert.True(DppControlledElements.Read("[1,2,3]").IsInvalid);
        }

        [Fact]
        public void Merge_MalformedExistingBlob_Throws()
        {
            // Rewriting an unreadable blob would discard a mapping we cannot see, silently turning
            // controlled elements public.
            Assert.Throws<InvalidOperationException>(
                () => DppControlledElements.Merge("{ \"ns=1;i=1\": \"new\" }", "not json"));
        }

        [Fact]
        public void Merge_PreservesControlledElementsFromExistingBlob()
        {
            const string existing = """
            { "ns=1;i=1": "old", "controlledElements": { "materials.billOfMaterials": "Recycler" } }
            """;
            const string freshNodeValues = """
            { "ns=1;i=1": "new" }
            """;

            string merged = DppControlledElements.Merge(freshNodeValues, existing);

            DppControlledElements.MappingResult map = DppControlledElements.Read(merged);
            Assert.Equal(DppControlledElements.MappingState.Valid, map.State);
            Assert.Single(map.Entries);
            Assert.Equal(s_recycler, map.Entries["materials.billOfMaterials"]);
            Assert.Contains("\"new\"", merged);
        }
    }
}
