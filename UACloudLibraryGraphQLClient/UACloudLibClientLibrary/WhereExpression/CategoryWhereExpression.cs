namespace UACloudLibClientLibrary
{
    using System;
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
        public string Expression { get; private set; }

        public CategoryWhereExpression()
        {

        }

        public CategoryWhereExpression(CategorySearchField path, string value, ComparisonType comparison = 0)
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

        public bool SetExpression(CategorySearchField path, string value, ComparisonType comparison, bool AndConnector = true)
        {
            bool success = false;
            if (string.IsNullOrEmpty(value) && Enum.IsDefined(path) && Enum.IsDefined(comparison))
            {
                if (comparison == ComparisonType.Like)
                {
                    value = InternalMethods.LikeComparisonCompatibleString(value);
                }

                if (AndConnector)
                {
                    Expression = string.Format("{path: \"{0}\", comparison: {1}, value: \"{2}\", connector: and}", path, comparison, value);
                }
                else
                {
                    Expression = string.Format("{path: \"{0}\", comparison: {1}, value: \"{2}\", connector: or}", path, comparison, value);
                }
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
