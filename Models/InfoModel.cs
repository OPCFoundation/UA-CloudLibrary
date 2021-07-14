
using System;

namespace UACloudLibrary
{
    public enum AddressSpaceLicense
    {
        MIT,
        ApacheLicense20,
        Custom
    }

    public class AddressSpace
    {
        public AddressSpace()
        {
            ID = string.Empty;
            Title = string.Empty;
            Version = new Version("1.0.0");
            License = AddressSpaceLicense.Custom;
            CopyrightText = string.Empty;
            CreationTimeStamp = DateTime.UtcNow;
            LastModification = DateTime.UtcNow;
            Contributor = new Organisation();
            Description = string.Empty;
            Category = new AddresSpaceCategory();
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
            AdditionalProperties = null;
        }

        public string ID { get; set; }

        public string Title { get; set; }

        public Version Version { get; set; }

        public AddressSpaceLicense License { get; set; }

        public string CopyrightText {get;set;}

        public DateTime CreationTimeStamp { get; set; }

        public DateTime LastModification { get; set; }

        public Organisation Contributor { get; set; }

        public string Description { get; set; }

        public AddresSpaceCategory Category { get; set; }

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
            CreationTimeStamp = DateTime.UtcNow;
            LastModification = DateTime.UtcNow;
        }

        public string ID { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Uri LogoUrl { get; set; }

        public string ContactEmail { get; set; }

        public Uri Website { get; set; }

        public DateTime CreationTimeStamp { get; set; }

        public DateTime LastModification { get; set; }
    }

    public class AddresSpaceCategory
    {
        public AddresSpaceCategory()
        {
            ID = string.Empty;
            Name = string.Empty;
            Description = null;
            IconUrl = null;
            CreationTimeStamp = DateTime.UtcNow;
            LastModification = DateTime.UtcNow;
        }

        public string ID { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Uri IconUrl { get; set; }

        public DateTime CreationTimeStamp { get; set; }

        public DateTime LastModification { get; set; }
    }

    public class AddressSpaceNodeset2
    {
        public AddressSpaceNodeset2()
        {
            AddressSpaceID = string.Empty;
            NodesetXml = string.Empty;
            CreationTimeStamp = DateTime.UtcNow;
            LastModification = DateTime.UtcNow;
        }

        public string AddressSpaceID { get; set; }

        public string NodesetXml { get; set; }

        public DateTime CreationTimeStamp { get; set; }

        public DateTime LastModification { get; set; }
    }
}
