using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.CloudLib.Client.Models
{
    /// <summary>GraphQL Result for datatype queries</summary>
    [JsonObject("datatype")]
    public class DatatypeResult
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <value>The identifier.</value>
        [JsonProperty("datatype_id")]
        public int ID { get; set; }
        /// <summary>Gets or sets the nodeset identifier.</summary>
        /// <value>The nodeset identifier.</value>
        [JsonProperty("nodeset_id")]
        public long NodesetID { get; set; }
        /// <summary>Gets or sets the browsename.</summary>
        /// <value>The browsename.</value>
        [JsonProperty("datatype_browsename")]
        public string Browsename { get; set; }
        /// <summary>Gets or sets the value.</summary>
        /// <value>The value.</value>
        [JsonProperty("datatype_value")]
        public string Value { get; set; }
        /// <summary>Gets or sets the namespace.</summary>
        /// <value>The namespace.</value>
        [JsonProperty("datatype_namespace")]
        public string Namespace { get; set; }
    }
    /// <summary>GraphQL Result for metadata queries</summary>
    [JsonObject("metadata")]
    public class MetadataResult
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [JsonProperty("metadata_id")]
        public int ID { get; set; }
        /// <summary>Gets or sets the nodeset identifier.</summary>
        /// <value>The nodeset identifier.</value>
        [JsonProperty("nodeset_id")]
        public long NodesetID { get; set; }
        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        [JsonProperty("metadata_name")]
        public string Name { get; set; }
        /// <summary>Gets or sets the value.</summary>
        /// <value>The value.</value>
        [JsonProperty("metadata_value")]
        public string Value { get; set; }
    }
    /// <summary>GraphQL Result for object queries</summary>
    [JsonObject("objecttype")]
    public class ObjectResult
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <value>The identifier.</value>
        [JsonProperty("objecttype_id")]
        public int ID { get; set; }
        /// <summary>Gets or sets the nodeset identifier.</summary>
        /// <value>The nodeset identifier.</value>
        [JsonProperty("nodeset_id")]
        public long NodesetID { get; set; }
        /// <summary>Gets or sets the browsename.</summary>
        /// <value>The browsename.</value>
        [JsonProperty("objecttype_browsename")]
        public string Browsename { get; set; }
        /// <summary>Gets or sets the value.</summary>
        /// <value>The value.</value>
        [JsonProperty("objecttype_value")]
        public string Value { get; set; }
        /// <summary>Gets or sets the namespace.</summary>
        /// <value>The namespace.</value>
        [JsonProperty("objecttype_namespace")]
        public string Namespace { get; set; }
    }
    /// <summary>GraphQL Result for reference queries</summary>
    [JsonObject("reference")]
    public class ReferenceResult
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <value>The identifier.</value>
        [JsonProperty("reference_id")]
        public int ID { get; set; }
        /// <summary>Gets or sets the nodeset identifier.</summary>
        /// <value>The nodeset identifier.</value>
        [JsonProperty("nodeset_id")]
        public long NodesetID { get; set; }
        /// <summary>Gets or sets the browsename.</summary>
        /// <value>The browsename.</value>
        [JsonProperty("reference_browsename")]
        public string Browsename { get; set; }
        /// <summary>Gets or sets the value.</summary>
        /// <value>The value.</value>
        [JsonProperty("reference_value")]
        public string Value { get; set; }
        /// <summary>Gets or sets the namespace.</summary>
        /// <value>The namespace.</value>
        [JsonProperty("reference_namespace")]
        public string Namespace { get; set; }
    }
    /// <summary>GraphQL Result for variable queries</summary>
    [JsonObject("variabletype")]
    public class VariableResult
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <value>The identifier.</value>
        [JsonProperty("variabletype_id")]
        public int ID { get; set; }
        /// <summary>Gets or sets the nodeset identifier.</summary>
        /// <value>The nodeset identifier.</value>
        [JsonProperty("nodeset_id")]
        public long NodesetID { get; set; }
        /// <summary>Gets or sets the browsename.</summary>
        /// <value>The browsename.</value>
        [JsonProperty("variabletype_browsename")]
        public string Browsename { get; set; }
        /// <summary>Gets or sets the value.</summary>
        /// <value>The value.</value>
        [JsonProperty("variabletype_value")]
        public string Value { get; set; }
        /// <summary>Gets or sets the namespace.</summary>
        /// <value>The namespace.</value>
        [JsonProperty("vaiabletype_namespace")]
        public string Namespace { get; set; }
    }
}
