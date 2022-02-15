namespace UACloudLibClientLibrary
{
    using System;
    using System.Runtime.InteropServices;

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
        Like
    }
    public interface IWhereExpression<T> where T : Enum
    {
        public string Expression { get; }
        /// <summary>
        /// Returns a string that is formatted so that it can be used in a where expression in a query
        /// </summary>
        /// <returns>A String formatted like this: {path: "---", comparison: ---, value: "---"}</returns>
        public bool SetExpression(T path, string value, ComparisonType comparison, [Optional] bool connector);
    }
}
