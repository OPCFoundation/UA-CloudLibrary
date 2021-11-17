using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UACloudLibClientLibrary
{
    public interface IInfoModell
    {
        string PropertiesToString();
    }

    public enum AddressSpaceLicense
    {
        MIT,
        ApacheLicense20,
        Custom
    }

    
    /// <summary>
    /// Contains the metadata of the nodeset and the nodeset itself
    /// </summary>
    public class AddressSpace : IInfoModell
    {
        [JsonProperty("iD")]
        public string ID {
            get; 
            set; 
        }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("license")]
        public AddressSpaceLicense License { get; set; }
        [JsonProperty("copyrightText")]
        public string CopyrightText { get; set; }
        [JsonProperty("creationTime")]
        public DateTime CreationTimeStamp { get; set; }
        [JsonProperty("lastModificationTime")]
        public DateTime LastModification { get; set; }
        [JsonProperty("contributor")]
        public Organisation Contributor{ get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("category")]
        public AddressSpaceCategory Category { get; set; }
        [JsonProperty("nodeset")]
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
        [JsonProperty("purchasingInformationUrl")]
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
        [JsonProperty("additionalProperties")]
        public Tuple<string, string>[] AdditionalProperties { get; set; }
        public string PropertiesToString()
        {
            string strTemp = $"ID: {ID}\n";
            strTemp += $"Title: {Title}\n";
            strTemp += $"Description: {Description}\n";
            strTemp += $"IconUrl: {IconUrl}\n";
            strTemp += $"Version: {Version}\n";
            strTemp += $"Contributor: \n[\n{Contributor.PropertiesToString()}]\n";
            strTemp += $"CreationTimeStamp: {CreationTimeStamp}\n";
            strTemp += $"LastModification: {LastModification}\n";
            return strTemp;
        }
    }
 
    /// <summary>
    /// Contains the metadata for the contributor/organisation
    /// </summary>
    [JsonObject("contributor")]
    public class Organisation : IInfoModell
    {
        [JsonProperty("iD")]
        public string ID { get; set; }
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
        public DateTime CreationTimeStamp { get; set; }
        [JsonProperty("lastModificationTime")]
        public DateTime LastModification { get; set; }

        public string PropertiesToString()
        {
            string strTemp = $"ID: {ID}\n";
            strTemp += $"name: {Name}\n";
            strTemp += $"Description: {Description}\n";
            strTemp += $"LogoUrl: {LogoUrl}\n";
            strTemp += $"ContactEmail: {ContactEmail}\n";
            strTemp += $"Website: {Website}\n";
            strTemp += $"CreationTimeStamp: {CreationTimeStamp}\n";
            strTemp += $"LastModification: {LastModification}\n";
            return strTemp;
        }
    }
    
    /// <summary>
    /// Defines the category
    /// </summary>
    [JsonObject("category")]
    public class AddressSpaceCategory : IInfoModell
    {
        [JsonProperty("iD")]
        public string ID { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("iconUrl")]
        public Uri IconUrl { get; set; }
        [JsonProperty("creationTime")]
        public DateTime CreationTimeStamp { get; set; }
        [JsonProperty("lastModificationTime")]
        public DateTime LastModificationTime { get; set; }

        public string PropertiesToString()
        {
            string strTemp = $"ID: {ID}\n";
            strTemp += $"name: {Name}\n";
            strTemp += $"Description: {Description}\n";
            strTemp += $"IconUrl: {IconUrl}\n";
            strTemp += $"CreationTimeStamp: {CreationTimeStamp}\n";
            strTemp += $"LastModification: {LastModificationTime}\n";
            return strTemp;
        }
    }
    
    /// <summary>
    /// Contains the nodeset and timestamps
    /// </summary>
    [JsonObject("nodeset")]
    public class AddressSpaceNodeset2
    {
        [JsonProperty("addressSpaceID")]
        public string AddressSpaceID { get; set; }
        [JsonProperty("nodesetXml")]
        public string NodesetXml { get; set; }
        [JsonProperty("creationTime")]
        public DateTime CreationTimeStamp { get; set; }
        [JsonProperty("lastModificationTime")]
        public DateTime LastModification { get; set; }
    }
}