using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Opc.Ua.Cloud.Client.Models;
using Opc.Ua.Cloud.Client;

namespace CloudLibClient.Tests
{
    internal sealed class RequiredModelInfoComparer : IEqualityComparer<RequiredModelInfo>
    {
        public bool Equals(RequiredModelInfo x, RequiredModelInfo y)
        {
            if (x == null && y == null) return true;
            if (x == null) return false;
            if (y == null) return false;
            return x.NamespaceUri == y.NamespaceUri && x.PublicationDate == y.PublicationDate && x.Version == y.Version;
        }

        public int GetHashCode([DisallowNull] RequiredModelInfo obj)
        {
            // not used by the tests
            throw new System.NotImplementedException();
        }
    }
}
