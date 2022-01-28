using System;
using System.Collections.Generic;
using System.Text;


namespace UACloudLibClientLibrary
{
    public enum CategorySearchField
    {
        iD,
        name,
        lastModification,
        creationTimeStamp,
        description
    }
    /// <summary>
    /// Defining the attributes the category must have
    /// </summary>
    public class CategoryWhereExpression : IWhereExpression<CategorySearchField>
    {
        public CategorySearchField Path { get; set; }
        public string Value { get; set; }
        public ComparisonType Comparison { get; set; }

        public CategoryWhereExpression()
        {

        }

        public CategoryWhereExpression(CategorySearchField path, string value, ComparisonType comparison = 0)
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
