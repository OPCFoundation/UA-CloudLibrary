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
        public AddressSpaceSearchField Path { get; set; }
        public string Value { get; set; }
        public ComparisonType Comparison { get; set; }

        public AddressSpaceWhereExpression()
        {

        }

        public AddressSpaceWhereExpression(AddressSpaceSearchField propertyName, string value, ComparisonType comparison = 0)
        {
            this.Path = propertyName;
            this.Comparison = comparison;
            this.Value = value;
        }

        public string GetExpression()
        {
            string asString = "{";

            switch (Path)
            {
                case AddressSpaceSearchField.contributorId:
                    {
                        asString += $"path: \"contributor.iD\", comparison: {Comparison}, value: \"{Value}\"";
                        break;
                    }
                case AddressSpaceSearchField.contributorName:
                    {
                        asString += $"path: \"contributor.name\", comparison: {Comparison}, value: \"{Value}\"";
                        break;
                    }
                case AddressSpaceSearchField.categoryId:
                    {
                        asString += $"path: \"category.iD\", comparison: {Comparison}, value: \"{Value}\"";
                        break;
                    }
                case AddressSpaceSearchField.categoryName:
                    {
                        asString += $"path: \"category.name\", comparison: {Comparison}, value: \"{Value}\"";
                        break;
                    }
                default:
                    {
                        asString += $"path: \"{Path}\", comparison: {Comparison}, value: \"{Value}\"";
                        break;
                    }
            }
                
            asString += "}";
            return asString;
        }
    }
}
