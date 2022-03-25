using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UACloudLibClientLibrary
{
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
        public string Title { get; set; }
        public string Version { get; set; }
        public AddressSpaceLicense License { get; set; }
        public string CopyrightText { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastModificationTime { get; set; }
        public Organisation Contributor { get; set; }
        public string Description { get; set; }
        public AddressSpaceCategory Category { get; set; }
        public AddressSpaceNodeset2 Nodeset { get; set; }
        /// <summary>
        /// If the address space is a official UA Information Model this refers to the OPC XYZ number of the published document. E.g. "OPC 40001-1" for the "UA CS for Machinery Part 1 - Basic Building Blocks"
        /// </summary>
        public Uri DocumentationUrl { get; set; }
        public Uri IconUrl { get; set; }
        public Uri LicenseUrl { get; set; }
        public string[] KeyWords { get; set; }
        public Uri PurchasingInformationUrl { get; set; }
        public Uri ReleaseNotesUrl { get; set; }
        public Uri TestSpecificationUrl { get; set; }
        /// <summary>
        /// Supported ISO language codes
        /// </summary>
        public string[] SupportedLocales { get; set; }
        public uint NumberOfDownloads { get; set; }
        public Tuple<string, string>[] AdditionalProperties { get; set; }
    }

    /// <summary>
    /// Contains the metadata for the contributor/organisation
    /// </summary>
    public class Organisation
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Uri LogoUrl { get; set; }
        public string ContactEmail { get; set; }
        public Uri Website { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastModificationTime { get; set; }
    }

    /// <summary>
    /// Defines the category
    /// </summary>
    public class AddressSpaceCategory
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Uri IconUrl { get; set; }
        public DateTime CreationTimeStamp { get; set; }
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