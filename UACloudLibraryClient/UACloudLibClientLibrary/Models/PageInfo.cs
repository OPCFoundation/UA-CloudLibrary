using Newtonsoft.Json;
using System.Collections.Generic;

namespace UACloudLibClientLibrary
{
    /// <summary>
    /// Contains a List of T, the total count of T available and if a next and/or previous page is available
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PageInfo<T> where T : class
    {
        [JsonProperty("edges")]
        public List<PageItem<T>> Items { get; set; }

        [JsonProperty("pageInfo")]
        public PageBools Page { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// Contains the data if a next and/or previous page is available
    /// </summary>
    public class PageBools
    {
        [JsonProperty("hasNextPage")]
        public bool hasNext { get; set; }

        [JsonProperty("hasPreviousPage")]
        public bool hasPrev { get; set; }
    }
}
