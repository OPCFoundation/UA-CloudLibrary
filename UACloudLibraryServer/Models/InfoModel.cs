
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UACloudLibrary
{
    public enum AddressSpaceLicense
    {
        MIT,
        ApacheLicense20,
        Custom
    }
    [Table("AddressSpaces")]
    public class AddressSpace
    {
        public AddressSpace()
        {
            ID = string.Empty;
            Title = string.Empty;
            Version = "1.0.0";
            License = AddressSpaceLicense.Custom;
            CopyrightText = string.Empty;
            CreationTime = DateTime.UtcNow;
            LastModificationTime = DateTime.UtcNow;
            Contributor = new Organisation();
            Description = string.Empty;
            Category = new AddressSpaceCategory();
            Nodeset = new AddressSpaceNodeset2();
            DocumentationUrl = null;
            IconUrl = null;
            LicenseUrl = null;
            Keywords = new string[0];
            PurchasingInformationUrl = null;
            ReleaseNotesUrl = null;
            TestSpecificationUrl = null;
            SupportedLocales = null;
            NumberOfDownloads = 0;
            //AdditionalProperties = null;
        }
        [Key]
        [Required]
        public string ID { get; set; }
        [Required]
        public string Title { get; set; }

        public string Version { get; set; }
        [Required]
        public AddressSpaceLicense License { get; set; }

        public string CopyrightText {get; set;}

        public DateTime CreationTime { get; set; }

        public DateTime LastModificationTime { get; set; }

        // Specifically used for efcore related data loading
        [ForeignKey("Contributor")]
        public string ContributorID { get; set; }

        [Required]
        public Organisation Contributor { get; set; }

        public string Description { get; set; }

        // Specifically used for efcore related data loading
        [ForeignKey("Category")]
        public string CategoryID { get; set; }

        [Required]
        public AddressSpaceCategory Category { get; set; }

        [Required]
        public AddressSpaceNodeset2 Nodeset { get; set; }

        /// <summary>
        /// Link to additional documentation, specifications, GitHub, etc.
        /// For example, If the address space is based on a standard or official UA Information Model, this links to the standard or the OPC specification URL.
        /// </summary>
        public Uri DocumentationUrl { get; set; }

        public Uri IconUrl { get; set; }

        public Uri LicenseUrl { get; set; }

        public string[] Keywords { get; set; }

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
    [Table("Organisations")]
    public class Organisation
    {
        public Organisation()
        {
            ID = string.Empty;
            Name = string.Empty;
            Description = null;
            LogoUrl = null;
            ContactEmail = null;
            Website = null;
            CreationTime = DateTime.UtcNow;
            LastModificationTime = DateTime.UtcNow;
        }
        [Key]
        [Required]
        public string ID { get; set; }
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public Uri LogoUrl { get; set; }

        public string ContactEmail { get; set; }

        public Uri Website { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime LastModificationTime { get; set; }
    }
    [Table("AddressSpaceCategories")]
    public class AddressSpaceCategory
    {
        public AddressSpaceCategory()
        {
            ID = string.Empty;
            Name = string.Empty;
            Description = null;
            IconUrl = null;
            CreationTime = DateTime.UtcNow;
            LastModificationTime = DateTime.UtcNow;
        }
        [Key]
        [Required]
        public string ID { get; set; }
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public Uri IconUrl { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime LastModificationTime { get; set; }
    }
    [Table("AddressSpaceNodesets")]
    public class AddressSpaceNodeset2
    {
        public AddressSpaceNodeset2()
        {
            AddressSpaceID = string.Empty;
            NodesetXml = string.Empty;
            CreationTime = DateTime.UtcNow;
            LastModificationTime = DateTime.UtcNow;
        }
        [Key]
        [ForeignKey("AddressSpace")]
        public string AddressSpaceID { get; set; }

        public string NodesetXml { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime LastModificationTime { get; set; }
    }
}
