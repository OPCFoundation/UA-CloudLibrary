using System;
using System.Collections.Generic;
using System.Text;
using UACloudLibClientLibrary.Models;

namespace UACloudLibClientLibrary
{
    public enum MetadataField
    {
        metadata_name,
        metadata_value
    }

    class MetadataWhereExpression : IWhereExpression<MetadataField>
    {
        public MetadataField Path { get; set; }
        public string Value { get; set; }
        public ComparisonType Comparison { get; set; }

        public MetadataWhereExpression(MetadataField propertyName, string value, ComparisonType comparison = 0)
        {
            this.Path = propertyName;
            this.Comparison = comparison;
            this.Value = value;
        }

        public string GetExpression()
        {
            return string.Format("{path: \"{0}\", comparison: {1}, value: \"{2}\"", Path, Comparison, Value);
        }
    }
}
