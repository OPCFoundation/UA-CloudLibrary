namespace UACloudLibClientLibrary
{
    using System;
    internal static class InternalMethods
    {
        public static string LikeComparisonCompatibleString(string value)
        {
            string result = "";
            if (!string.IsNullOrEmpty(value))
            {
                if (value.StartsWith("%") && value.EndsWith("%"))
                {
                    result = value;
                }
                else
                {
                    result = string.Format("%" + value + "%");
                }
            }
            else
            {
                throw new ArgumentNullException("Parameter 'value' is null");
            }
            return result;
        }
    }
}
