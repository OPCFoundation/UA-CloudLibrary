using System;
using System.Collections.Generic;
using System.Text;

namespace UACloudLibClientLibrary
{
    public enum OrganisationSearchField
    {
        iD,
        name
    }
    /// <summary>
    /// Defining the attributes the Contributor/Organisation must have
    /// </summary>
    public class OrganisationWhereExpression : IWhereExpression<OrganisationSearchField>
    {
        public OrganisationSearchField Path { get; set; }
        public string Value { get; set; }
        public ComparisonType Comparison { get; set; }

        public OrganisationWhereExpression()
        {

        }

        public OrganisationWhereExpression(OrganisationSearchField path, string value, ComparisonType comparison = 0)
        {
            Path = path;
            Value = value;
            Comparison = comparison;
        }
 
        public string GetExpression()
        {
            string asString = "{";
            asString += $"path: \"{Path}\", comparison: {Comparison}, value: \"{Value}\"";
            asString += "}";
            return asString;
        }
    }
}
