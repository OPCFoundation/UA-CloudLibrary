/* ========================================================================
 * Copyright (c) 2005-2025 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;

namespace Opc.Ua.Cloud.Library
{
    public class NodesetViewerNode : IComparable<NodesetViewerNode>
    {
        public string Id { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public string BrowseName { get; set; } = string.Empty;

        public string NodeClass { get; set; } = string.Empty;

        public List<NodesetViewerNode> Children { get; set; }

        /// <summary>
        /// True once the children of this node have been browsed from the server.
        /// Distinguishes a loaded leaf (empty Children) from a node that has not been browsed yet.
        /// </summary>
        public bool ChildrenLoaded { get; set; }

        public string Value { get; set; } = string.Empty;

        public int CompareTo(NodesetViewerNode other)
        {
            if (ReferenceEquals(other, null))
            {
                return 1;
            }

            return string.Compare(Id ?? string.Empty, other.Id ?? string.Empty, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            return !(obj is NodesetViewerNode other) || string.Equals(Id, other.Id, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return (Id ?? string.Empty).GetHashCode(StringComparison.Ordinal);
        }

        public static bool operator ==(NodesetViewerNode left, NodesetViewerNode right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(NodesetViewerNode left, NodesetViewerNode right)
        {
            return !(left == right);
        }

        public static bool operator <(NodesetViewerNode left, NodesetViewerNode right)
        {
            return ReferenceEquals(left, null) ? !ReferenceEquals(right, null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(NodesetViewerNode left, NodesetViewerNode right)
        {
            return ReferenceEquals(left, null) || left.CompareTo(right) <= 0;
        }

        public static bool operator >(NodesetViewerNode left, NodesetViewerNode right)
        {
            return !ReferenceEquals(left, null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(NodesetViewerNode left, NodesetViewerNode right)
        {
            return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.CompareTo(right) >= 0;
        }
    }
}
