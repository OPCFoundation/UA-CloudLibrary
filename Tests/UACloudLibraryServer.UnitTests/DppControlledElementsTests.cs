using System.Collections.Generic;

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
                "urn:ref:bom": [ "Recycler", "Repairer" ],
                "urn:ref:supplier": "Customs"
              }
            }
            """;

            IReadOnlyDictionary<string, string[]> map = DppControlledElements.Parse(values);

            Assert.Equal(2, map.Count);
            Assert.Equal(s_recyclerRepairer, map["urn:ref:bom"]);
            Assert.Equal(s_customs, map["urn:ref:supplier"]);
        }

        [Fact]
        public void Parse_NoControlledElements_ReturnsEmpty()
        {
            Assert.Empty(DppControlledElements.Parse("{ \"ns=1;i=1\": \"42\" }"));
            Assert.Empty(DppControlledElements.Parse(null));
            Assert.Empty(DppControlledElements.Parse("not json"));
        }

        [Fact]
        public void Merge_PreservesControlledElementsFromExistingBlob()
        {
            const string existing = """
            { "ns=1;i=1": "old", "controlledElements": { "urn:ref:bom": "Recycler" } }
            """;
            const string freshNodeValues = """
            { "ns=1;i=1": "new" }
            """;

            string merged = DppControlledElements.Merge(freshNodeValues, existing);

            IReadOnlyDictionary<string, string[]> map = DppControlledElements.Parse(merged);
            Assert.Single(map);
            Assert.Equal(s_recycler, map["urn:ref:bom"]);
            Assert.Contains("\"new\"", merged);
        }
    }
}
