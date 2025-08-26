namespace AdminShell
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.Serialization;

    [DataContract]
    public class PagedResult<T>
    {
        [DataMember(Name = "result")]
        public List<T> Result { get; set; }

        [DataMember(Name = "paging_metadata")]
        public PagedResultMetadata Metadata { get; set; }
    }

    public static class PagedResult
    {
        public static PagedResult<T> ToPagedList<T>(List<T> sourceList, PaginationParameters paginationParameters)
        {
            List<T> outputList = new();

            if (sourceList?.Count > 0)
            {
                int startIndex = paginationParameters.Cursor;
                int endIndex = Math.Min(sourceList.Count - 1, paginationParameters.Limit - 1);

                if (startIndex > endIndex)
                {
                    throw new ArgumentException($"Requested pagination start index ({startIndex}) is greater than the size of the source list ({sourceList.Count}).");
                }

                // Build the outputList with the requested range
                for (int i = startIndex; i <= endIndex; i++)
                {
                    outputList.Add(sourceList[i]);
                }
            }

            return new PagedResult<T>() { Result = outputList, Metadata = new PagedResultMetadata() { Cursor = (paginationParameters.Cursor + outputList.Count - 1).ToString(CultureInfo.InvariantCulture) } };
        }
    }
}
