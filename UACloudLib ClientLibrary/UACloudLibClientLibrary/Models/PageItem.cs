using Newtonsoft.Json;

namespace UACloudLibClientLibrary
{
    /// <summary>
    /// Contains T and its cursor
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PageItem<T> where T : class
    {
        [JsonProperty("cursor")]
        public string Cursor { get; set; }
        [JsonProperty("node")]
        public T Item {  get; set; }
    }
}