using System.Collections.Generic;
using System.Linq;

using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Models;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Regression tests for how a passport template's node tree is split into data elements.
    /// </summary>
    /// <remarks>
    /// Templates built on the OPC UA DPP information model place each data element as an Object
    /// directly under the DigitalProductPassport object, with ElementId, DictionaryReference and
    /// Value as Variable properties. The service only understood an "Elements" container whose leaf
    /// nodes carried their own value, so such passports were served with no elements at all.
    /// </remarks>
    public class DppElementTreeTests
    {
        private static readonly string[] s_batteryTopLevel = { "ns=1;i=5001", "ns=1;i=5062" };
        private static readonly string[] s_identityMembers = { "ns=1;i=5002", "ns=1;i=5006" };
        private static readonly string[] s_legacyMembers = { "ns=1;i=7001", "ns=1;i=7002" };

        private const string DppNs = "http://opcfoundation.org/UA/DigitalProductPassport/";

        private static NodesetViewerNode Obj(string id, string name) =>
            new() { Id = id, BrowseName = name, Text = name, NodeClass = "Object" };

        private static NodesetViewerNode Typed(string id, string name, uint dppTypeId) =>
            new() { Id = id, BrowseName = name, Text = name, NodeClass = "Object", TypeDefinition = $"nsu={DppNs};i={dppTypeId}" };

        private static NodesetViewerNode Var(string id, string name) =>
            new() { Id = id, BrowseName = name, Text = name, NodeClass = "Variable" };

        [Fact]
        public void BrowsedTypeDefinitionUsesTheFormTheClassifierMatches()
        {
            // UAClient renders a browsed node's TypeDefinition this way; the classifier keys on it.
            var namespaces = new Opc.Ua.NamespaceTable();
            ushort index = namespaces.GetIndexOrAppend(DppNs);

            string rendered = Opc.Ua.NodeId.ToExpandedNodeId(new Opc.Ua.NodeId(15u, index), namespaces).ToString();

            Assert.Equal($"nsu={DppNs};i=15", rendered);
        }

        [Fact]
        public void MultiValuedElementIsClassifiedByTypeNotAsACollection()
        {
            // Catena-X templates: a MultiValuedDataElement holds its item template as a child Object.
            var element = new DPPService.ElementNode(Typed("ns=1;i=10", "childParts", 15), SpecModel: true);
            DPPService.ElementNodeDetail detail = DPPService.DescribeElement(element, new List<NodesetViewerNode> {
                Var("ns=1;i=11", "ElementId"),
                Typed("ns=1;i=12", "childParts", 19),
            });

            Assert.Equal(DPPService.ElementKind.MultiValued, DPPService.ClassifyElement(element, detail));
            Assert.Single(detail.Children);
        }

        [Fact]
        public void MultiLanguageElementIsClassifiedByTypeAndWritesItsValueProperty()
        {
            var element = new DPPService.ElementNode(Typed("ns=1;i=33", "technicianComment", 27), SpecModel: true);
            DPPService.ElementNodeDetail detail = DPPService.DescribeElement(element, new List<NodesetViewerNode> {
                Var("ns=1;i=34", "ElementId"),
                Var("ns=1;i=36", "Value"),
            });

            Assert.Equal(DPPService.ElementKind.MultiLanguage, DPPService.ClassifyElement(element, detail));
            Assert.Equal("ns=1;i=36", detail.ValueNode?.Id);
        }

        [Fact]
        public void EmptyTypedCollectionStaysACollection()
        {
            var element = new DPPService.ElementNode(Typed("ns=1;i=40", "empty", 19), SpecModel: true);
            DPPService.ElementNodeDetail detail = DPPService.DescribeElement(element, new List<NodesetViewerNode> { Var("ns=1;i=41", "ElementId") });

            Assert.Equal(DPPService.ElementKind.Collection, DPPService.ClassifyElement(element, detail));
        }

        [Fact]
        public void ElementOfUnknownTypeIsClassifiedByStructure()
        {
            var element = new DPPService.ElementNode(Obj("ns=1;i=50", "custom"), SpecModel: true);
            DPPService.ElementNodeDetail detail = DPPService.DescribeElement(element, new List<NodesetViewerNode> { Var("ns=1;i=51", "Value") });

            Assert.Equal(DPPService.ElementKind.SingleValued, DPPService.ClassifyElement(element, detail));
        }

        [Fact]
        public void MultiLanguageValueParsesTheEn18223Shape()
        {
            List<MultiLanguageValue> values = DPPService.ParseMultiLanguageValue("[{\"value\":\"Hallo\",\"language\":\"de\"},{\"value\":\"Hello\",\"language\":\"en\"}]");

            Assert.Equal(2, values.Count);
            Assert.Equal(("Hallo", "de"), (values[0].Value, values[0].Language));
            Assert.Equal(("Hello", "en"), (values[1].Value, values[1].Language));
        }

        [Fact]
        public void MultiLanguageValueToleratesEmptyAndPlainText()
        {
            Assert.Empty(DPPService.ParseMultiLanguageValue(string.Empty));

            MultiLanguageValue single = Assert.Single(DPPService.ParseMultiLanguageValue("plain"));
            Assert.Equal("plain", single.Value);
            Assert.Equal(string.Empty, single.Language);
        }

        [Fact]
        public void SpecModelTopLevelElementsAreTheObjectsUnderThePassport()
        {
            // Shape of the "Industrial Battery Passport 2027-02" template's DigitalProductPassport object.
            var dppChildren = new List<NodesetViewerNode> {
                Var("ns=1;i=5239", "Granularity"),
                Var("ns=1;i=5240", "UniqueProductIdentifier"),
                Var("ns=1;i=5245", "LastUpdated"),
                Obj("ns=1;i=5001", "Identity"),
                Obj("ns=1;i=5062", "Sustainability"),
            };

            List<DPPService.ElementNode> elements = DPPService.SpecModelTopLevelElements(dppChildren);

            Assert.Equal(s_batteryTopLevel, elements.Select(e => e.Node.Id));
            Assert.All(elements, e => Assert.True(e.SpecModel));
        }

        [Fact]
        public void SpecModelCollectionExposesOnlyObjectChildrenAsElements()
        {
            var identity = new DPPService.ElementNode(Obj("ns=1;i=5001", "Identity"), SpecModel: true);
            var children = new List<NodesetViewerNode> {
                Var("ns=1;i=5300", "ElementId"),
                Obj("ns=1;i=5002", "DP001_Unique_identifier_unique_product_identifier"),
                Obj("ns=1;i=5006", "DP002_Manufacturer"),
            };

            DPPService.ElementNodeDetail detail = DPPService.DescribeElement(identity, children);

            Assert.Equal(s_identityMembers, detail.Children.Select(c => c.Node.Id));
            Assert.Null(detail.ValueNode);
            Assert.Equal("ns=1;i=5300", Assert.Single(detail.Properties).Id);
        }

        [Fact]
        public void SpecModelSingleValuedElementReadsAndWritesItsValueProperty()
        {
            var element = new DPPService.ElementNode(Obj("ns=1;i=5002", "DP001_Unique_identifier_unique_product_identifier"), SpecModel: true);
            var children = new List<NodesetViewerNode> {
                Var("ns=1;i=5003", "ElementId"),
                Var("ns=1;i=5004", "DictionaryReference"),
                Var("ns=1;i=5005", "Value"),
            };

            DPPService.ElementNodeDetail detail = DPPService.DescribeElement(element, children);

            Assert.Empty(detail.Children);
            Assert.Equal("ns=1;i=5005", detail.ValueNode?.Id);
            Assert.Equal(3, detail.Properties.Count);
        }

        [Fact]
        public void SpecModelRelatedResourceHasNoSingleValue()
        {
            var element = new DPPService.ElementNode(Obj("ns=1;i=6000", "Manual"), SpecModel: true);
            var children = new List<NodesetViewerNode> {
                Var("ns=1;i=6001", "ElementId"),
                Var("ns=1;i=6002", "ContentType"),
                Var("ns=1;i=6003", "Url"),
            };

            DPPService.ElementNodeDetail detail = DPPService.DescribeElement(element, children);

            Assert.Empty(detail.Children);
            Assert.Null(detail.ValueNode);
            Assert.Contains(detail.Properties, p => p.BrowseName == "Url");
        }

        [Fact]
        public void LegacyLeafCarriesItsValueOnTheElementNode()
        {
            var leaf = new DPPService.ElementNode(Var("ns=1;i=7001", "maxPressure"), SpecModel: false);

            DPPService.ElementNodeDetail detail = DPPService.DescribeElement(leaf, null);

            Assert.Empty(detail.Children);
            Assert.Same(leaf.Node, detail.ValueNode);
            Assert.Empty(detail.Properties);
        }

        [Fact]
        public void LegacyCollectionTreatsEveryChildAsAnElement()
        {
            var collection = new DPPService.ElementNode(Obj("ns=1;i=7000", "pressure"), SpecModel: false);
            var children = new List<NodesetViewerNode> {
                Var("ns=1;i=7001", "maxPressure"),
                Var("ns=1;i=7002", "Value"),
            };

            DPPService.ElementNodeDetail detail = DPPService.DescribeElement(collection, children);

            Assert.Equal(s_legacyMembers, detail.Children.Select(c => c.Node.Id));
            Assert.All(detail.Children, c => Assert.False(c.SpecModel));
            Assert.Null(detail.ValueNode);
        }
    }
}
