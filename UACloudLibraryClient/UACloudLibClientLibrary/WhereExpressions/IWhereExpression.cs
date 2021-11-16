using System;
using System.Collections.Generic;
using System.Text;

namespace UACloudLibClientLibrary
{
    public enum ComparisonType
    {
        Equal,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanEqual,
        Contains,
        StartsWith,
        EndsWith,
        //In,
        Like
    }

    public interface IWhereExpression<T> where T : Enum
    {
        public T Path { get; set; }
        public string Value { get; set; }
        public ComparisonType Comparison { get; set; }
        /// <summary>
        /// Returns a string that is formatted so that it can be used in a where expression in a query
        /// </summary>
        /// <returns>A String formatted like this: {path: "---", comparison: ---, value: "---"}</returns>
        public string GetExpression();
    }
}
