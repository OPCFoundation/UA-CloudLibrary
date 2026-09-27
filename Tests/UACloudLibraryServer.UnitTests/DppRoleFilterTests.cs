using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Logging.Abstractions;

using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Models;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Regression tests for role filtering across every element container type.
    /// </summary>
    /// <remarks>
    /// The filter and the path resolver must agree on what counts as a container. They did not:
    /// resolution descended both <see cref="DataElementCollection"/> and
    /// <see cref="MultiValuedDataElement"/>, while the filter descended only the former. A controlled
    /// element nested under a multi-valued element was therefore addressable by the API but never
    /// pruned, so it stayed in the response body and was signed into the ESDC.
    /// </remarks>
    public class DppRoleFilterTests
    {
        private const string ControlledPath = "batch.secret";

        private static readonly IReadOnlyDictionary<string, string[]> s_controlled =
            new Dictionary<string, string[]> { [ControlledPath] = new[] { "Auditor" } };

        // Only _accessPolicy and _logger are touched by the filtering path under test.
        private static DPPService CreateService() =>
            new(null, null, null, null, new DppAccessPolicy(), NullLoggerFactory.Instance);

        private static DppControlledElements.MappingResult Mapping()
        {
            var json = new JsonObject {
                ["controlledElements"] = new JsonObject { [ControlledPath] = "Auditor" }
            };

            return DppControlledElements.Read(json.ToJsonString());
        }

        private static SingleValuedDataElement Leaf(string id, string value) => new()
        {
            ElementId = id,
            Value = JsonValue.Create(value)
        };

        private static readonly string[] s_auditor = { "Auditor" };
        private static readonly string[] s_publicOnly = { "public" };
        private static readonly string[] s_publicAndSecret = { "public", "secret" };
        private static readonly string[] s_x = { "x" };
        private static readonly string[] s_y = { "y" };

        private static DigitalProductPassport DppWith(DataElement root) => new()
        {
            DigitalProductPassportId = "dpp-1",
            UniqueProductIdentifier = "prod-1",
            DppSchemaVersion = "1.0",
            DppStatus = "active",
            LastUpdate = System.DateTimeOffset.UtcNow,
            EconomicOperatorId = "EO-1",
            Elements = new List<DataElement> { root }
        };

        private static IEnumerable<string> ElementIdsUnder(DataElement element) =>
            DPPService.ChildElementsOf(element)?.Select(e => e.ElementId) ?? Enumerable.Empty<string>();

        [Fact]
        public void ControlledChild_UnderMultiValuedElement_IsRemovedForUnauthorizedCaller()
        {
            var multi = new MultiValuedDataElement {
                ElementId = "batch",
                Value = new List<DataElement> { Leaf("public", "visible"), Leaf("secret", "classified") }
            };

            DigitalProductPassport filtered = CreateService()
                .FilterForRoles(DppWith(multi), System.Array.Empty<string>(), Mapping());

            DataElement root = Assert.Single(filtered.Elements);
            Assert.Equal("batch", root.ElementId);

            string[] remaining = ElementIdsUnder(root).ToArray();
            Assert.Equal(s_publicOnly, remaining);
        }

        [Fact]
        public void ControlledChild_UnderMultiValuedElement_IsKeptForAuthorizedCaller()
        {
            var multi = new MultiValuedDataElement {
                ElementId = "batch",
                Value = new List<DataElement> { Leaf("public", "visible"), Leaf("secret", "classified") }
            };

            DigitalProductPassport filtered = CreateService()
                .FilterForRoles(DppWith(multi), s_auditor, Mapping());

            string[] remaining = ElementIdsUnder(Assert.Single(filtered.Elements)).ToArray();
            Assert.Equal(s_publicAndSecret, remaining);
        }

        [Fact]
        public void Filtering_DoesNotMutateTheSourceElement()
        {
            // The DPP instance comes from the browsed/cached model, so pruning in place would leak
            // one caller's role filtering into the next caller's response.
            var multi = new MultiValuedDataElement {
                ElementId = "batch",
                Value = new List<DataElement> { Leaf("public", "visible"), Leaf("secret", "classified") }
            };

            DigitalProductPassport source = DppWith(multi);

            CreateService().FilterForRoles(source, System.Array.Empty<string>(), Mapping());

            Assert.Equal(2, multi.Value.Count);
            Assert.Contains(multi.Value, e => e.ElementId == "secret");
        }

        [Fact]
        public void ChildElementsOf_CoversEveryContainerType()
        {
            // Pins the shared containment definition. A new container type added to the model must
            // be reflected here, which is what keeps resolution and filtering from diverging again.
            var coll = new DataElementCollection {
                ElementId = "c",
                Elements = new List<DataElement> { Leaf("x", "1") }
            };

            var multi = new MultiValuedDataElement {
                ElementId = "m",
                Value = new List<DataElement> { Leaf("y", "2") }
            };

            Assert.Equal(s_x, ElementIdsUnder(coll).ToArray());
            Assert.Equal(s_y, ElementIdsUnder(multi).ToArray());
            Assert.Null(DPPService.ChildElementsOf(Leaf("leaf", "3")));
        }
    }
}
