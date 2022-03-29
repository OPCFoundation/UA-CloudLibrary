using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Opc.Ua.CloudLib.Client
{
    /// <summary>
    /// The license under which the AddressSpace was published
    /// </summary>
    public enum AddressSpaceLicense
    {
        /// <summary>
        /// MIT License
        /// </summary>
        MIT,
        /// <summary>
        /// Apache 2.0 License
        /// </summary>
        ApacheLicense20,
        /// <summary>
        /// Custom License model <see cref="AddressSpace.LicenseUrl"/>
        /// </summary>
        Custom
    }


    /// <summary>Contains the metadata of the nodeset and the nodeset itself</summary>
    public class AddressSpace
    {
        /// <summary>Create a new AddressSpace metadata</summary>
        public AddressSpace()
        {
            this.Nodeset = new AddressSpaceNodeset2();
            this.Contributor = new Organisation();
            this.Category = new AddressSpaceCategory();
        }
        /// <summary>Used for identifying during conversion and when trying to download the nodeset</summary>
        public string MetadataID { get; set; }
        /// <summary>Gets or sets the title.</summary>
        /// <value>The title.</value>
        public string Title { get; set; }
        /// <summary>Gets or sets the version.</summary>
        /// <value>The version.</value>
        public string Version { get; set; }
        /// <summary>Gets or sets the license.</summary>
        /// <value>The license.</value>
        public AddressSpaceLicense License { get; set; }
        /// <summary>Gets or sets the copyright text.</summary>
        /// <value>The copyright text.</value>
        public string CopyrightText { get; set; }
        /// <summary>Gets or sets the creation time.</summary>
        /// <value>The creation time.</value>
        public DateTime CreationTime { get; set; }
        /// <summary>Gets or sets the last modification time.</summary>
        /// <value>The last modification time.</value>
        public DateTime LastModificationTime { get; set; }
        /// <summary>Gets or sets the contributor.</summary>
        /// <value>The contributor.</value>
        public Organisation Contributor{ get; set; }
        /// <summary>Gets or sets the description.</summary>
        /// <value>The description.</value>
        public string Description { get; set; }
        /// <summary>Gets or sets the category.</summary>
        /// <value>The category.</value>
        public AddressSpaceCategory Category { get; set; }
        /// <summary>Gets or sets the nodeset.</summary>
        /// <value>The nodeset.</value>
        public AddressSpaceNodeset2 Nodeset { get; set; }
        /// <summary>
        /// If the address space is a official UA Information Model this refers to the OPC XYZ number of the published document. E.g. "OPC 40001-1" for the "UA CS for Machinery Part 1 - Basic Building Blocks"
        /// </summary>
        public Uri DocumentationUrl { get; set; }
        /// <summary>Gets or sets the icon URL.</summary>
        /// <value>The icon URL.</value>
        public Uri IconUrl { get; set; }
        /// <summary>Gets or sets the license URL.</summary>
        /// <value>The license URL.</value>
        public Uri LicenseUrl { get; set; }
        /// <summary>Gets or sets the key words.</summary>
        /// <value>The key words.</value>
        public string[] KeyWords { get; set; }
        /// <summary>Gets or sets the purchasing information URL.</summary>
        /// <value>The purchasing information URL.</value>
        public Uri PurchasingInformationUrl { get; set; }
        /// <summary>Gets or sets the release notes URL.</summary>
        /// <value>The release notes URL.</value>
        public Uri ReleaseNotesUrl { get; set; }
        /// <summary>Gets or sets the test specification URL.</summary>
        /// <value>The test specification URL.</value>
        public Uri TestSpecificationUrl { get; set; }
        /// <summary>
        /// Supported ISO language codes
        /// </summary>
        public string[] SupportedLocales { get; set; }
        /// <summary>Gets or sets the number of downloads.</summary>
        /// <value>The number of downloads.</value>
        public uint NumberOfDownloads { get; set; }
        /// <summary>Gets or sets the additional properties.</summary>
        /// <value>The additional properties.</value>
        public Tuple<string, string>[] AdditionalProperties { get; set; }
    }
 
    /// <summary>
    /// Contains the metadata for the contributor/organisation
    /// </summary>
    public class Organisation 
    {
        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>Gets or sets the description.</summary>
        /// <value>The description.</value>
        public string Description { get; set; }
        /// <summary>Gets or sets the logo URL.</summary>
        /// <value>The logo URL.</value>
        public Uri LogoUrl { get; set; }
        /// <summary>Gets or sets the contact email.</summary>
        /// <value>The contact email.</value>
        public string ContactEmail { get; set; }
        /// <summary>Gets or sets the website.</summary>
        /// <value>The website.</value>
        public Uri Website { get; set; }
        /// <summary>Gets or sets the creation time.</summary>
        /// <value>The creation time.</value>
        public DateTime CreationTime { get; set; }
        /// <summary>Gets or sets the last modification time.</summary>
        /// <value>The last modification time.</value>
        public DateTime LastModificationTime { get; set; }
    }
    
    /// <summary>
    /// Defines the category
    /// </summary>
    public class AddressSpaceCategory
    {
        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>Gets or sets the description.</summary>
        /// <value>The description.</value>
        public string Description { get; set; }
        /// <summary>Gets or sets the icon URL.</summary>
        /// <value>The icon URL.</value>
        public Uri IconUrl { get; set; }
        /// <summary>Gets or sets the creation time stamp.</summary>
        /// <value>The creation time stamp.</value>
        public DateTime CreationTimeStamp { get; set; }
        /// <summary>Gets or sets the last modification time.</summary>
        /// <value>The last modification time.</value>
        public DateTime LastModificationTime { get; set; }
    }
    
    /// <summary>
    /// Contains the nodeset and timestamps
    /// </summary>
    public class AddressSpaceNodeset2
    {
        /// <summary>Gets or sets the nodeset XML.</summary>
        /// <value>The nodeset XML.</value>
        public string NodesetXml { get; set; }
        /// <summary>Gets or sets the creation time stamp.</summary>
        /// <value>The creation time stamp.</value>
        public DateTime CreationTimeStamp { get; set; }
        /// <summary>Gets or sets the last modification.</summary>
        /// <value>The last modification.</value>
        public DateTime LastModification { get; set; }
    }
}