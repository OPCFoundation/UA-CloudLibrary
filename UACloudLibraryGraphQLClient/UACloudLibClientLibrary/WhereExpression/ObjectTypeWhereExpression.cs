using System;
using System.Collections.Generic;
using System.Text;

namespace UACloudLibClientLibrary
{
    public enum ObjectTypeFields
    {
        objecttype_name,
        objecttype_value
    }
    class ObjectTypeWhereExpression : IWhereExpression<ObjectTypeFields>
    {
        public ObjectTypeFields Path { get; set; }
        public string Value { get; set; }
        public ComparisonType Comparison { get; set; }

        public ObjectTypeWhereExpression(ObjectTypeFields field, string value, ComparisonType comparison = 0)
        {
            Path = field;
            Value = value;
            Comparison = comparison;
        }

        public string GetExpression()
        {
            return string.Format("{path: \"{0}\", comparison: {1}, value: \"{2}\"", Path, Comparison, Value);
        }
    }
}
