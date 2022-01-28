/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace UACloudLibrary
{
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public enum AddressSpaceLicense
    {
        MIT,
        ApacheLicense20,
        Custom
    }

    [Keyless]
    public class AddressSpace
    {
        public AddressSpace()
        {
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
            SupportedLocales = new string[0];
            NumberOfDownloads = 0;
            AdditionalProperties = new Tuple<string, string>[0];
        }

        [Key]
        [Column("addressspaceid")]
        public int AddressSpaceId { get; set; }

        [Required]
        [Column("title")]
        public string Title { get; set; }

        [Required]
        [Column("versionnumber")]
        public string Version { get; set; }

        [Required]
        [Column("license")]
        public AddressSpaceLicense License { get; set; }

        [Required]
        [Column("copyrighttext")]
        public string CopyrightText {get; set;}

        [Column("creationtime")]
        public DateTime CreationTime { get; set; }

        [Column("lastmodificationtime")]
        public DateTime LastModificationTime { get; set; }

        [Required]
        public Organisation Contributor { get; set; }

        [Required]
        [Column("description")]
        public string Description { get; set; }

        [Required]
        public AddressSpaceCategory Category { get; set; }

        [Required]
        public AddressSpaceNodeset2 Nodeset { get; set; }

        /// <summary>
        /// Link to additional documentation, specifications, GitHub, etc.
        /// For example, If the address space is based on a standard or official UA Information Model, this links to the standard or the OPC specification URL.
        /// </summary>
        [Column("documentationurl")]
        public Uri DocumentationUrl { get; set; }

        [Column("iconurl")]
        public Uri IconUrl { get; set; }

        [Column("licenseurl")]
        public Uri LicenseUrl { get; set; }

        [Column("keywords")]
        public string[] Keywords { get; set; }

        [Column("purchasinginformationurl")]
        public Uri PurchasingInformationUrl { get; set; }

        [Column("releasenotesurl")]
        public Uri ReleaseNotesUrl { get; set; }

        [Column("testspecificationurl")]
        public Uri TestSpecificationUrl { get; set; }

        /// <summary>
        /// Supported ISO language codes
        /// </summary>
        [Column("supportedlocales")]
        public string[] SupportedLocales { get; set; }

        [Column("numberofdownloads")]
        public uint NumberOfDownloads { get; set; }

        public Tuple<string, string>[] AdditionalProperties { get; set; }
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
            CreationTime = DateTime.UtcNow;
            LastModificationTime = DateTime.UtcNow;
        }

        [Key]
        [Column("contributorid")]
        public int ContributorId { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("logourl")]
        public Uri LogoUrl { get; set; }

        [Column("contactemail")]
        public string ContactEmail { get; set; }

        [Column("website")]
        public Uri Website { get; set; }

        [Column("creationtime")]
        public DateTime CreationTime { get; set; }

        [Column("lastmodificationtime")]
        public DateTime LastModificationTime { get; set; }
    }

    public class AddressSpaceCategory
    {
        public AddressSpaceCategory()
        {
            Name = string.Empty;
            Description = null;
            IconUrl = null;
            CreationTime = DateTime.UtcNow;
            LastModificationTime = DateTime.UtcNow;
        }
        [Key]
        [Column("categoryid")]
        public int CategoryId { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("iconurl")]
        public Uri IconUrl { get; set; }

        [Column("creationtime")]
        public DateTime CreationTime { get; set; }

        [Column("lastmodificationtime")]
        public DateTime LastModificationTime { get; set; }
    }

    public class AddressSpaceNodeset2
    {
        public AddressSpaceNodeset2()
        {
            NodesetXml = string.Empty;
            CreationTime = DateTime.UtcNow;
            LastModificationTime = DateTime.UtcNow;
        }

        [Required]
        public string NodesetXml { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime LastModificationTime { get; set; }
    }
}
