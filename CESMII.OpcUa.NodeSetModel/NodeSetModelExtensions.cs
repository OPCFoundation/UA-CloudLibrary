using System;
using System.Collections.Generic;
using System.Linq;

namespace CESMII.OpcUa.NodeSetModel
{
    public static class NodeSetModelExtensions
    { 
        public static IEnumerable<NodeModel> EnumerateAllNodes(this NodeSetModel _this)
        {
            var allNodes = _this.DataTypes
                .Concat<NodeModel>(_this.VariableTypes)
                .Concat<NodeModel>(_this.Interfaces)
                .Concat<NodeModel>(_this.ObjectTypes)
                .Concat<NodeModel>(_this.Properties)
                .Concat<NodeModel>(_this.DataVariables)
                .Concat<NodeModel>(_this.Objects)
                .Concat<NodeModel>(_this.Methods)
                .Concat<NodeModel>(_this.ReferenceTypes)
                .Concat<NodeModel>(_this.UnknownNodes)
                ;
            return allNodes;
        }
        public static void UpdateAllNodes(this NodeSetModel _this)
        {
            _this.AllNodesByNodeId.Clear();
            foreach(var node in EnumerateAllNodes(_this))
            {
                _this.AllNodesByNodeId.TryAdd(node.NodeId, node);

            }
        }
        /// <summary>
        /// Ensures that all indices in the model are consistent, including
        /// - SuperType/SubType collections
        /// - Properties, DataVariables, Objects etc. collections
        /// </summary>
        /// <param name="_this">NodeSetModel to update.</param>
        public static void UpdateIndices(this NodeSetModel _this)
        {
            _this.AllNodesByNodeId.Clear();
            var updatedNodes = new HashSet<string>();
            foreach(var node in EnumerateAllNodes(_this))
            {
                node.UpdateIndices(_this, updatedNodes);
            }
        }
    }
#if NETSTANDARD2_0
    public static class DictionaryExtensions
    {
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict.ContainsKey(key)) return false;
            dict.Add(key, value);
            return true;
        }
    }

    internal class HashCode
    {
        public static int Combine<T1, T2, T3>(T1 o1, T2 o2, T3 o3)
        {
            return (o1.GetHashCode() ^ o2.GetHashCode() ^ o3.GetHashCode());
        }
    }

#endif
}