using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Opc.Ua;
using Opc.Ua.Cloud.Library;
using Opc.Ua.Export;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Some nodesets declare a parent/child link only on the child (an inverse HasProperty to the
    /// parent). Forward browsing from the parent then finds no children, so the importer must add
    /// the missing forward reference.
    /// </summary>
    public class NodesetReverseReferenceTests
    {
        private const string NodesetTemplate = @"<?xml version=""1.0"" encoding=""utf-8""?>
<UANodeSet xmlns=""http://opcfoundation.org/UA/2011/03/UANodeSet.xsd"">
  <NamespaceUris><Uri>http://example.org/test/</Uri></NamespaceUris>
  <Models><Model ModelUri=""http://example.org/test/"" Version=""1.0.0"" PublicationDate=""2025-01-01T00:00:00Z"" /></Models>
  <Aliases>
    <Alias Alias=""String"">i=12</Alias>
    <Alias Alias=""HasTypeDefinition"">i=40</Alias>
    <Alias Alias=""HasProperty"">i=46</Alias>
  </Aliases>
  <UAObject NodeId=""ns=1;i=1"" BrowseName=""1:Element"">
    <DisplayName>Element</DisplayName>
    <References>
      <Reference ReferenceType=""HasTypeDefinition"">i=58</Reference>
      {0}
    </References>
  </UAObject>
  <UAVariable NodeId=""ns=1;i=2"" BrowseName=""1:DictionaryReference"" DataType=""String"" ParentNodeId=""ns=1;i=1"">
    <DisplayName>DictionaryReference</DisplayName>
    <References>
      <Reference ReferenceType=""HasTypeDefinition"">i=68</Reference>
      <Reference ReferenceType=""HasProperty"" IsForward=""false"">ns=1;i=1</Reference>
    </References>
    <Value><String xmlns=""http://opcfoundation.org/UA/2008/02/Types.xsd"">urn:example:1</String></Value>
  </UAVariable>
</UANodeSet>";

        [Fact]
        public void AddsForwardReferenceWhenOnlyInverseIsDeclared()
        {
            (SystemContext context, NodeStateCollection nodes, Dictionary<NodeId, NodeState> byId) = Import(string.Empty);
            NodeState parent = byId.Values.Single(n => n.BrowseName.Name == "Element");
            NodeState child = byId.Values.Single(n => n.BrowseName.Name == "DictionaryReference");
            Assert.Empty(ForwardTargets(context, parent));

            int added = NodesetFileNodeManager.AddMissingForwardReferences(context, nodes, Lookup(byId));

            Assert.Equal(1, added);
            Assert.Equal(new[] { child.NodeId }, ForwardTargets(context, parent));
        }

        [Fact]
        public void IsIdempotent()
        {
            (SystemContext context, NodeStateCollection nodes, Dictionary<NodeId, NodeState> byId) = Import(string.Empty);

            NodesetFileNodeManager.AddMissingForwardReferences(context, nodes, Lookup(byId));
            int addedAgain = NodesetFileNodeManager.AddMissingForwardReferences(context, nodes, Lookup(byId));

            Assert.Equal(0, addedAgain);
            NodeState parent = byId.Values.Single(n => n.BrowseName.Name == "Element");
            Assert.Single(ForwardTargets(context, parent));
        }

        [Fact]
        public void DoesNotDuplicateExistingForwardReference()
        {
            (SystemContext context, NodeStateCollection nodes, Dictionary<NodeId, NodeState> byId) =
                Import(@"<Reference ReferenceType=""HasProperty"">ns=1;i=2</Reference>");
            NodeState parent = byId.Values.Single(n => n.BrowseName.Name == "Element");

            int added = NodesetFileNodeManager.AddMissingForwardReferences(context, nodes, Lookup(byId));

            Assert.Equal(0, added);
            Assert.Single(ForwardTargets(context, parent));
        }

        [Fact]
        public void IgnoresTargetsOutsideTheManager()
        {
            (SystemContext context, NodeStateCollection nodes, _) = Import(string.Empty);

            int added = NodesetFileNodeManager.AddMissingForwardReferences(context, nodes, _ => null);

            Assert.Equal(0, added);
        }

        private static (SystemContext, NodeStateCollection, Dictionary<NodeId, NodeState>) Import(string extraParentReference)
        {
            string xml = NodesetTemplate.Replace("{0}", extraParentReference, System.StringComparison.Ordinal);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            UANodeSet nodeSet = UANodeSet.Read(stream);

            var context = new SystemContext(null) { NamespaceUris = new NamespaceTable(), ServerUris = new StringTable() };
            var nodes = new NodeStateCollection();
            nodeSet.Import(context, nodes);

            var byId = new Dictionary<NodeId, NodeState>();
            foreach (NodeState node in nodes)
            {
                Collect(context, node, byId);
            }

            return (context, nodes, byId);
        }

        private static void Collect(ISystemContext context, NodeState node, Dictionary<NodeId, NodeState> byId)
        {
            byId[node.NodeId] = node;
            var children = new List<BaseInstanceState>();
            node.GetChildren(context, children);
            foreach (BaseInstanceState child in children)
            {
                Collect(context, child, byId);
            }
        }

        private static System.Func<NodeId, NodeState> Lookup(Dictionary<NodeId, NodeState> byId) =>
            id => byId.TryGetValue(id, out NodeState state) ? state : null;

        // Children reachable by a forward browse: explicit forward HasProperty references plus
        // property children the importer attached directly to the parent.
        private static List<NodeId> ForwardTargets(ISystemContext context, NodeState parent)
        {
            var references = new List<IReference>();
            parent.GetReferences(context, references);
            var targets = references
                .Where(r => !r.IsInverse && r.ReferenceTypeId == ReferenceTypeIds.HasProperty)
                .Select(r => (NodeId)r.TargetId)
                .ToList();

            var children = new List<BaseInstanceState>();
            parent.GetChildren(context, children);
            targets.AddRange(children.OfType<BaseVariableState>().Select(c => c.NodeId));
            return targets;
        }
    }
}
