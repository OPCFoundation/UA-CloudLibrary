using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Opc.Ua.Cloud.Library.Client;

namespace CloudLibClient.Tests
{
    internal class OrganisationComparer : IEqualityComparer<Organisation>
    {
        public bool Equals(Organisation x, Organisation y)
        {
            if (x == null && y == null) return true;
            if (x == null) return false;
            if (y == null) return false;
            return x.Name == y.Name
                && (x.Description == y.Description || (string.IsNullOrEmpty(x.Description) && string.IsNullOrEmpty(y.Description)))
                && x.LogoUrl?.ToString() == y.LogoUrl?.ToString()
                && (x.ContactEmail == y.ContactEmail || (string.IsNullOrEmpty(x.ContactEmail) && string.IsNullOrEmpty(y.ContactEmail)))
                && x.Website?.ToString() == y.Website?.ToString();
        }

        public int GetHashCode([DisallowNull] Organisation obj)
        {
            // not used by the tests
            throw new System.NotImplementedException();
        }
    }

    internal class CategoryComparer : IEqualityComparer<Category>
    {
        public bool Equals(Category x, Category y)
        {
            if (x == null && y == null) return true;
            if (x == null) return false;
            if (y == null) return false;
            return x.Name == y.Name
                && (x.Description == y.Description || (string.IsNullOrEmpty(x.Description) && string.IsNullOrEmpty(y.Description)))
                && x.IconUrl?.ToString() == y.IconUrl?.ToString();
        }

        public int GetHashCode([DisallowNull] Category obj)
        {
            // not used by the tests
            throw new System.NotImplementedException();
        }
    }

    internal class RequiredModelInfoComparer : IEqualityComparer<RequiredModelInfo>
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
    internal class UAPropertyComparer : IEqualityComparer<UAProperty>
    {
        public bool Equals(UAProperty x, UAProperty y)
        {
            if (x == null && y == null) return true;
            if (x == null) return false;
            if (y == null) return false;
            return x.Name == y.Name && x.Value == y.Value;
        }

        public int GetHashCode([DisallowNull] UAProperty obj)
        {
            // not used by the tests
            throw new System.NotImplementedException();
        }
    }
}
