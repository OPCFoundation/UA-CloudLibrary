namespace UACloudLibClientLibrary
{
    using Newtonsoft.Json;
    using System;

    public enum AddressSpaceLicense
    {
        MIT,
        ApacheLicense20,
        Custom
    }



    /// <summary>
    /// Contains the metadata of the nodeset and the nodeset itself
    /// </summary>
    public class AddressSpace
    {
        public AddressSpace()
        {
            this.Nodeset = new AddressSpaceNodeset2();
            this.Contributor = new Organisation();
            this.Category = new AddressSpaceCategory();
        }
        /// <summary>
        /// Used for identifying during conversion and when trying to download the nodeset
        /// </summary>
        public string MetadataID { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("license")]
        public AddressSpaceLicense License { get; set; }
        [JsonProperty("copyrightText")]
        public string CopyrightText { get; set; }
        [JsonProperty("creationTime")]
        public DateTime CreationTime { get; set; }
        [JsonProperty("lastModificationTime")]
        public DateTime LastModificationTime { get; set; }
        [JsonProperty("contributor")]
        public Organisation Contributor { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("category")]
        public AddressSpaceCategory Category { get; set; }
        public AddressSpaceNodeset2 Nodeset { get; set; }
        /// <summary>
        /// If the address space is a official UA Information Model this refers to the OPC XYZ number of the published document. E.g. "OPC 40001-1" for the "UA CS for Machinery Part 1 - Basic Building Blocks"
        /// </summary>
        [JsonProperty("documentationUrl")]
        public Uri DocumentationUrl { get; set; }
        [JsonProperty("iconUrl")]
        public Uri IconUrl { get; set; }
        [JsonProperty("licenseUrl")]
        public Uri LicenseUrl { get; set; }
        [JsonProperty("keywords")]
        public string[] KeyWords { get; set; }
        [JsonProperty("purchasinginformationUrl")]
        public Uri PurchasingInformationUrl { get; set; }
        [JsonProperty("releaseNotesUrl")]
        public Uri ReleaseNotesUrl { get; set; }
        [JsonProperty("testSpecificationUrl")]
        public Uri TestSpecificationUrl { get; set; }
        /// <summary>
        /// Supported ISO language codes
        /// </summary>
        [JsonProperty("supportedLocales")]
        public string[] SupportedLocales { get; set; }
        [JsonProperty("numberOfDownloads")]
        public uint NumberOfDownloads { get; set; }
        public Tuple<string, string>[] AdditionalProperties { get; set; }
        [JsonProperty("nodesetId")]
        public long NodesetId { get; set; }
    }

    /// <summary>
    /// Contains the metadata for the contributor/organisation
    /// </summary>
    [JsonObject("contributor")]
    public class Organisation
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("logoUrl")]
        public Uri LogoUrl { get; set; }
        [JsonProperty("contactEmail")]
        public string ContactEmail { get; set; }
        [JsonProperty("website")]
        public Uri Website { get; set; }
        [JsonProperty("creationTime")]
        public DateTime CreationTime { get; set; }
        [JsonProperty("lastModificationTime")]
        public DateTime LastModificationTime { get; set; }
    }

    /// <summary>
    /// Defines the category
    /// </summary>
    [JsonObject("category")]
    public class AddressSpaceCategory
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("iconUrl")]
        public Uri IconUrl { get; set; }
        [JsonProperty("creationTime")]
        public DateTime CreationTime { get; set; }
        [JsonProperty("lastModificationTime")]
        public DateTime LastModificationTime { get; set; }
    }

    /// <summary>
    /// Contains the nodeset and timestamps
    /// </summary>
    public class AddressSpaceNodeset2
    {
        public string NodesetXml { get; set; }
        public DateTime CreationTimeStamp { get; set; }
        public DateTime LastModification { get; set; }
    }
}