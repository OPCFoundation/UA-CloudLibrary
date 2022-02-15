using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using Newtonsoft.Json;

namespace UACloudLibClientLibrary
{
    public enum AddressSpaceSearchField
    {
        addressspaceid,
        title,
        version,
        contributorId,
        contributorName,
        categoryId,
        categoryName,
        license,
        lastModified,
        creationTimeStamp
    }
    /// <summary>
    /// Defining the attributes the AddressSpace must have
    /// </summary>
    public class AddressSpaceWhereExpression : IWhereExpression<AddressSpaceSearchField>
    {
        public string Expression { get; private set; }

        public AddressSpaceWhereExpression()
        {

        }

        public AddressSpaceWhereExpression(AddressSpaceSearchField path, string value, ComparisonType comparison = 0)
        {
            if (SetExpression(path, value, comparison))
            {
                // succeeded
            }
            else
            {
                throw new Exception("One or more arguments was incorrect");
            }
        }

        public bool SetExpression(AddressSpaceSearchField path, string value, ComparisonType comparison, bool AndConnector = true)
        {
            bool success = false;
            if (string.IsNullOrEmpty(value) && Enum.IsDefined(path) && Enum.IsDefined(comparison))
            {
                if(comparison == ComparisonType.Like)
                {
                    value = InternalMethods.LikeComparisonCompatibleString(value);
                }

                string asString = "{";
                switch (path)
                {
                    case AddressSpaceSearchField.contributorName:
                        {
                            asString += $"path: \"name\", comparison: {comparison}, value: \"{value}\"";
                            break;
                        }
                    case AddressSpaceSearchField.categoryName:
                        {
                            asString += $"path: \"name\", comparison: {comparison}, value: \"{value}\"";
                            break;
                        }
                    default:
                        {
                            asString += $"path: \"{path}\", comparison: {comparison}, value: \"{value}\"";
                            break;
                        }
                }

                if (AndConnector)
                {
                    asString += ", connector: and";
                }
                else
                {
                    asString += ", connector: or";
                }

                asString += "}";
                Expression = asString;
                success = true;
            }
            else
            {
                success = false;
            }
            return success;
        }
    }
}
