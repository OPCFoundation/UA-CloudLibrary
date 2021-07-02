
using System;

namespace UA_CloudLibrary
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
            Version = string.Empty;
            License = AddressSpaceLicense.Custom;
            CreationTimeStamp = DateTime.UtcNow;
            LastModification = DateTime.UtcNow;
            Contributor = new Organisation();
            Description = string.Empty;
            Category = new AddresSpaceCategory();
            Nodeset = new AddressSpaceNodeset2();
            OPCFDocumentNumber = null;
            IconUrl = null;
            LicenseUrl = null;
            KeyWords = new string[0];
            PurchasingInformationUrl = null;
        }

        public string ID { get; set; }

        public string Title { get; set; }

        public string Version { get; set; }

        public AddressSpaceLicense License { get; set; }

        public DateTime CreationTimeStamp { get; set; }

        public DateTime LastModification { get; set; }

        public Organisation Contributor { get; set; }

        public string Description { get; set; }

        public AddresSpaceCategory Category { get; set; }

        public AddressSpaceNodeset2 Nodeset { get; set; }

        /// <summary>
        /// If the address space is a official UA Information Model this refers to the OPC XYZ number of the published document. E.g. "OPC 40001-1" for the "UA CS for Machinery Part 1 - Basic Building Blocks"
        /// </summary>
        public string OPCFDocumentNumber { get; set; }

        public string IconUrl { get; set; }

        public string LicenseUrl { get; set; }

        public string[] KeyWords { get; set; }

        public string PurchasingInformationUrl { get; set; }
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

        public string LogoUrl { get; set; }

        public string ContactEmail { get; set; }

        public string Website { get; set; }

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

        public string IconUrl { get; set; }

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
