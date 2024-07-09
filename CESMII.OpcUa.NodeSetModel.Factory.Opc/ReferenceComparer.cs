using Opc.Ua.Export;
using System.Collections.Generic;

namespace CESMII.OpcUa.NodeSetModel.Export.Opc
{
    internal class ReferenceComparer : IEqualityComparer<Reference>
    {
        public bool Equals(Reference r1, Reference r2)
        {
            return r1.IsForward == r2.IsForward
                && r1.Value == r2.Value
                && r1.ReferenceType == r2.ReferenceType;
        }

        public int GetHashCode(Reference r)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + r.IsForward.GetHashCode();
                hash = hash * 23 + r.Value.GetHashCode();
                hash = hash * 23 + r.ReferenceType.GetHashCode();
                return hash;
            }
        }
    }
}