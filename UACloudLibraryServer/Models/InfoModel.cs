/* Copyright (c) 1996-2022 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation Corporate Members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
*/

namespace UACloudLibrary
{
    using System;
    using System.ComponentModel.DataAnnotations;

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
            Title = string.Empty;
            Version = "1.0.0";
            License = AddressSpaceLicense.Custom;
            CopyrightText = string.Empty;
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
            SupportedLocales = new string[0];
            NumberOfDownloads = 0;
            AdditionalProperties = null;
        }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Version { get; set; }

        [Required]
        public AddressSpaceLicense License { get; set; }

        [Required]
        public string CopyrightText {get; set;}

        [Required]
        public Organisation Contributor { get; set; }

        [Required]
        public string Description { get; set; }

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

        public NodesetProperty[] AdditionalProperties { get; set; }
    }

    public class NodesetProperty
    {
        public string Name { get; set; }

        public string Value { get; set; }
    }

    public class Organisation
    {
        public Organisation()
        {
            Name = string.Empty;
            Description = null;
            LogoUrl = null;
            ContactEmail = null;
            Website = null;
        }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public Uri LogoUrl { get; set; }

        public string ContactEmail { get; set; }

        public Uri Website { get; set; }
    }

    public class AddressSpaceCategory
    {
        public AddressSpaceCategory()
        {
            Name = string.Empty;
            Description = null;
            IconUrl = null;
        }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public Uri IconUrl { get; set; }
    }

    public class AddressSpaceNodeset2
    {
        public AddressSpaceNodeset2()
        {
            NodesetXml = string.Empty;
            PublicationDate = DateTime.MinValue;
            LastModifiedDate = DateTime.MinValue;
        }

        [Required]
        public string NodesetXml { get; set; }

        public DateTime PublicationDate { get; set; }

        public DateTime LastModifiedDate { get; set; }
    }
}
