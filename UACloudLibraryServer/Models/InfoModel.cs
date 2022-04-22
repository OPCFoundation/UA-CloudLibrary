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
