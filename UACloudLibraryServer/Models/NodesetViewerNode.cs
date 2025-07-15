
using System;
using System.Collections.Generic;

namespace AdminShell
{
    public class NodesetViewerNode : IComparable<NodesetViewerNode>
    {
        public string Id { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public List<NodesetViewerNode> Children { get; set; }

        public string Value { get; set; } = string.Empty;

        public int CompareTo(NodesetViewerNode other)
        {
            return string.Compare(Text, other.Text, StringComparison.Ordinal);
        }
    }
}
