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
        public void Read_ExplicitNullMapping_IsInvalidNotAbsent()
        {
            // JsonNode.Parse represents an explicit null as a null reference, so a naive presence
            // check reports this as Absent - which callers treat as public-by-default, turning a
            // malformed access policy into a fail-open one.
            DppControlledElements.MappingResult result = DppControlledElements.Read("{ \"controlledElements\": null }");

            Assert.Equal(DppControlledElements.MappingState.Invalid, result.State);
            Assert.True(result.IsInvalid);
        }

        [Fact]
        public void Merge_ExplicitNullMapping_Throws()
        {
            // Rewriting an explicitly-null mapping to absent would downgrade a deny-all DPP to public.
            Assert.Throws<InvalidOperationException>(
                () => DppControlledElements.Merge("{ \"ns=1;i=1\": \"new\" }", "{ \"controlledElements\": null }"));
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

        // JSON allows several case variants of the same reserved name to coexist. The lookup is
        // case-insensitive, so silently taking the first would let a decoy empty policy shadow a
        // real one and make controlled elements public.
        [Theory]
        [InlineData("""
        { "controlledElements": {}, "ControlledElements": { "secret": "Auditor" } }
        """)]
        [InlineData("""
        { "ControlledElements": { "secret": "Auditor" }, "controlledElements": {} }
        """)]
        [InlineData("""
        { "controlledElements": { "a": "Auditor" }, "CONTROLLEDELEMENTS": { "b": "Recycler" } }
        """)]
        public void Read_MultipleCaseVariants_AreInvalidNotPublic(string values)
        {
            DppControlledElements.MappingResult result = DppControlledElements.Read(values);

            // Invalid means deny-all. Absent (the previous behaviour for the decoy-first ordering)
            // would have meant public-by-default.
            Assert.Equal(DppControlledElements.MappingState.Invalid, result.State);
            Assert.True(result.IsInvalid);
        }

        [Fact]
        public void Read_SingleNonCanonicalCasing_IsStillAccepted()
        {
            // One spelling is unambiguous regardless of casing, so it must keep working.
            const string values = """
            { "ControlledElements": { "supplierInfo": "Customs" } }
            """;

            DppControlledElements.MappingResult result = DppControlledElements.Read(values);

            Assert.Equal(DppControlledElements.MappingState.Valid, result.State);
            Assert.Equal(s_customs, result.Entries["supplierInfo"]);
        }

        [Fact]
        public void Merge_RefusesAmbiguousControlledElements()
        {
            // Re-attaching one copy would discard the others and convert Read's deny-all into
            // whatever the surviving copy permits.
            const string existing = """
            { "controlledElements": {}, "ControlledElements": { "secret": "Auditor" } }
            """;
            const string freshNodeValues = """
            { "ns=1;i=1": "new" }
            """;

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => DppControlledElements.Merge(freshNodeValues, existing));

            Assert.Contains("case-variant", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Merge_StripsEveryCaseVariantFromBrowseOutput()
        {
            // Browse output should never carry the reserved key, but if it carries several variants
            // they must all be removed - otherwise the merged document would be the ambiguous shape
            // that Read has to reject.
            const string freshNodeValues = """
            { "ns=1;i=1": "new", "controlledElements": { "stray": "Recycler" }, "ControlledElements": { "stray2": "Customs" } }
            """;
            const string existing = """
            { "controlledElements": { "supplierInfo": "Customs" } }
            """;

            string merged = DppControlledElements.Merge(freshNodeValues, existing);

            DppControlledElements.MappingResult map = DppControlledElements.Read(merged);
            Assert.Equal(DppControlledElements.MappingState.Valid, map.State);
            Assert.Single(map.Entries);
            Assert.Equal(s_customs, map.Entries["supplierInfo"]);
        }
    }
}
