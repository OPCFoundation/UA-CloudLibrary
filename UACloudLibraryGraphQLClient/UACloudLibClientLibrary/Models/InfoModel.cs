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

namespace UACloudLibrary.Models
{
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel.DataAnnotations;

    public enum License
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
            License = License.Custom;
            CopyrightText = string.Empty;
            Contributor = new Organisation();
            Description = string.Empty;
            Category = new Category();
            Nodeset = new Nodeset();
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
        [JsonProperty("title")]
        public string Title { get; set; }

        [Required]
        [JsonProperty("license")]
        public License License { get; set; }

        [Required]
        [JsonProperty("copyrightText")]
        public string CopyrightText { get; set; }

        [Required]
        [JsonProperty("contributor")]
        public Organisation Contributor { get; set; }

        [Required]
        [JsonProperty("description")]
        public string Description { get; set; }

        [Required]
        [JsonProperty("category")]
        public Category Category { get; set; }

        [Required]
        [JsonProperty("nodeset")]
        public Nodeset Nodeset { get; set; }

        /// <summary>
        /// Link to additional documentation, specifications, GitHub, etc.
        /// For example, If the address space is based on a standard or official UA Information Model, this links to the standard or the OPC specification URL.
        /// </summary>
        [JsonProperty("documentationUrl")]
        public Uri DocumentationUrl { get; set; }

        [JsonProperty("iconUrl")] 
        public Uri IconUrl { get; set; }

        [JsonProperty("licenseUrl")] 
        public Uri LicenseUrl { get; set; }

        [JsonProperty("keywords")] 
        public string[] Keywords { get; set; }

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
        public Property[] AdditionalProperties { get; set; }
    }

    public class Property
    {
        [JsonProperty("name")] 
        public string Name { get; set; }

        [JsonProperty("value")] 
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
    }

    public class Category
    {
        public Category()
        {
            Name = string.Empty;
            Description = null;
            IconUrl = null;
        }

        [Required]
        [JsonProperty("name")] 
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("iconUrl")] 
        public Uri IconUrl { get; set; }
    }

    public class Nodeset
    {
        public Nodeset()
        {
            NodesetXml = string.Empty;
            Identifier = 0;
            NamespaceUri = null;
            Version = string.Empty;
            PublicationDate = DateTime.MinValue;
            LastModifiedDate = DateTime.MinValue;
        }

        [Required]
        [JsonProperty("nodesetXml")] 
        public string NodesetXml { get; set; }

        [JsonProperty("identifier")]
        public uint Identifier { get; set; }

        [JsonProperty("namespaceUri")]
        public Uri NamespaceUri { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("publicationDate")]
        public DateTime PublicationDate { get; set; }

        [JsonProperty("lastModifiedDate")] 
        public DateTime LastModifiedDate { get; set; }
    }
}
