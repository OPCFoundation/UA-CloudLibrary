namespace UACloudLibClientLibrary
{
    internal static class InternalMethods
    {
        public static string LikeComparisonCompatibleString(string value)
        {
            string result = "";
            if(value.StartsWith("%") && value.EndsWith("%"))
            {
                result = value;
            }
            else
            {
                result = string.Format("%" + value + "%");
            }
            return result;
        }
    }
}
