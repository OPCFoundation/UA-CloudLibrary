using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.CloudLib.Client.Models
{
    [JsonObject("datatype")]
    public class DatatypeResult
    {
        [JsonProperty("datatype_id")]
        public int ID { get; set; }
        [JsonProperty("nodeset_id")]
        public long NodesetID { get; set; }
        [JsonProperty("datatype_browsename")]
        public string Browsename { get; set; }
        [JsonProperty("datatype_value")]
        public string Value { get; set; }
        [JsonProperty("datatype_namespace")]
        public string Namespace { get; set; }
    }
    [JsonObject("metadata")]
    public class MetadataResult
    {
        [JsonProperty("metadata_id")]
        public int ID { get; set; }
        [JsonProperty("nodeset_id")]
        public long NodesetID { get; set; }
        [JsonProperty("metadata_name")]
        public string Name { get; set; }
        [JsonProperty("metadata_value")]
        public string Value { get; set; }
    }
    [JsonObject("objecttype")]
    public class ObjectResult
    {
        [JsonProperty("objecttype_id")]
        public int ID { get; set; }
        [JsonProperty("nodeset_id")]
        public long NodesetID { get; set; }
        [JsonProperty("objecttype_browsename")]
        public string Browsename { get; set; }
        [JsonProperty("objecttype_value")]
        public string Value { get; set; }
        [JsonProperty("objecttype_namespace")]
        public string Namespace { get; set; }
    }
    [JsonObject("reference")]
    public class ReferenceResult
    {
        [JsonProperty("reference_id")]
        public int ID { get; set; }
        [JsonProperty("nodeset_id")]
        public long NodesetID { get; set; }
        [JsonProperty("reference_browsename")]
        public string Browsename { get; set; }
        [JsonProperty("reference_value")]
        public string Value { get; set; }
        [JsonProperty("reference_namespace")]
        public string Namespace { get; set; }
    }
    [JsonObject("variabletype")]
    public class VariableResult
    {
        [JsonProperty("variabletype_id")]
        public int ID { get; set; }
        [JsonProperty("nodeset_id")]
        public long NodesetID { get; set; }
        [JsonProperty("variabletype_browsename")]
        public string Browsename { get; set; }
        [JsonProperty("variabletype_value")]
        public string Value { get; set; }
        [JsonProperty("vaiabletype_namespace")]
        public string Namespace { get; set; }
    }
}
