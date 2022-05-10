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

namespace Opc.Ua.Cloud.Library.Client
{
    using System;
    using Newtonsoft.Json;

    /// <summary>License Enumeration</summary>
    public enum License
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
        /// Custom License model <see cref="UANameSpace.LicenseUrl"/>
        /// </summary>
        Custom
    }
    /// <summary>Contains the metadata of the nodeset and the nodeset itself</summary>
    public class UANameSpace
    {
        /// <summary>Create a new NameSpace metadata</summary>
        public UANameSpace()
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
            Keywords = Array.Empty<string>();
            PurchasingInformationUrl = null;
            ReleaseNotesUrl = null;
            TestSpecificationUrl = null;
            SupportedLocales = Array.Empty<string>();
            NumberOfDownloads = 0;
            AdditionalProperties = null;
        }

        /// <summary>Gets or sets the title.</summary>
        /// <value>The title.</value>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>Gets or sets the license.</summary>
        /// <value>The license.</value>
        [JsonProperty("license")]
        public License License { get; set; }

        /// <summary>Gets or sets the copyright text.</summary>
        /// <value>The copyright text.</value>
        [JsonProperty("copyrightText")]
        public string CopyrightText { get; set; }

        /// <summary>Gets or sets the contributor.</summary>
        /// <value>The contributor.</value>
        [JsonProperty("contributor")]
        public Organisation Contributor { get; set; }

        /// <summary>Gets or sets the description.</summary>
        /// <value>The description.</value>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>Gets or sets the category.</summary>
        /// <value>The category.</value>
        [JsonProperty("category")]
        public Category Category { get; set; }

        /// <summary>Gets or sets the nodeset.</summary>
        /// <value>The nodeset.</value>
        [JsonProperty("nodeset")]
        public Nodeset Nodeset { get; set; }

        /// <summary>
        /// Link to additional documentation, specifications, GitHub, etc.
        /// For example, If the address space is based on a standard or official UA Information Model, this links to the standard or the OPC specification URL.
        /// </summary>
        [JsonProperty("documentationUrl")]
        public Uri DocumentationUrl { get; set; }

        /// <summary>Gets or sets the icon URL.</summary>
        /// <value>The icon URL.</value>
        [JsonProperty("iconUrl")]
        public Uri IconUrl { get; set; }

        /// <summary>Gets or sets the license URL.</summary>
        /// <value>The license URL.</value>
        [JsonProperty("licenseUrl")]
        public Uri LicenseUrl { get; set; }

        /// <summary>Gets or sets the key words.</summary>
        /// <value>The key words.</value>
        [JsonProperty("keywords")]
        public string[] Keywords { get; set; }

        /// <summary>Gets or sets the purchasing information URL.</summary>
        /// <value>The purchasing information URL.</value>
        [JsonProperty("purchasingInformationUrl")]
        public Uri PurchasingInformationUrl { get; set; }

        /// <summary>Gets or sets the release notes URL.</summary>
        /// <value>The release notes URL.</value>
        [JsonProperty("releaseNotesUrl")]
        public Uri ReleaseNotesUrl { get; set; }

        /// <summary>Gets or sets the release notes URL.</summary>
        /// <value>The release notes URL.</value>
        [JsonProperty("testSpecificationUrl")]
        public Uri TestSpecificationUrl { get; set; }

        /// <summary>
        /// Supported ISO language codes
        /// </summary>
        [JsonProperty("supportedLocales")]
        public string[] SupportedLocales { get; set; }

        /// <summary>Gets or sets the number of downloads.</summary>
        /// <value>The number of downloads.</value>
        [JsonProperty("numberOfDownloads")]
        public uint NumberOfDownloads { get; set; }

        /// <summary>Gets or sets the validation status.</summary>
        /// <value>Status: Parsed, Validaded, Error + message</value>
        [JsonProperty("validationStatus")]
        public string ValidationStatus { get; set; }


        /// <summary>Gets or sets the additional properties.</summary>
        /// <value>The additional properties.</value>
        [JsonProperty("additionalProperties")]
        public UAProperty[] AdditionalProperties { get; set; }
    }

    /// <summary>Property Class</summary>
    public class UAProperty
    {
        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>Gets or sets the value.</summary>
        /// <value>The value.</value>
        [JsonProperty("value")]
        public string Value { get; set; }
    }
    /// <summary>
    /// Contains the metadata for the contributor/organisation
    /// </summary>
    public class Organisation
    {
        /// <summary>Initializes a new instance of the <see cref="Organisation" /> class.</summary>
        public Organisation()
        {
            Name = string.Empty;
            Description = null;
            LogoUrl = null;
            ContactEmail = null;
            Website = null;
        }

        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>Gets or sets the description.</summary>
        /// <value>The description.</value>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>Gets or sets the logo URL.</summary>
        /// <value>The logo URL.</value>
        [JsonProperty("logoUrl")]
        public Uri LogoUrl { get; set; }

        /// <summary>Gets or sets the contact email.</summary>
        /// <value>The contact email.</value>
        [JsonProperty("contactEmail")]
        public string ContactEmail { get; set; }

        /// <summary>Gets or sets the website.</summary>
        /// <value>The website.</value>
        [JsonProperty("website")]
        public Uri Website { get; set; }
    }

    /// <summary>Category Class</summary>
    public class Category
    {
        /// <summary>Initializes a new instance of the <see cref="Category" /> class.</summary>
        public Category()
        {
            Name = string.Empty;
            Description = null;
            IconUrl = null;
        }

        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>Gets or sets the description.</summary>
        /// <value>The description.</value>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>Gets or sets the icon URL.</summary>
        /// <value>The icon URL.</value>
        [JsonProperty("iconUrl")]
        public Uri IconUrl { get; set; }
    }

    /// <summary>Nodeset Class</summary>
    public class Nodeset
    {
        /// <summary>Initializes a new instance of the <see cref="Nodeset" /> class.</summary>
        public Nodeset()
        {
            NodesetXml = string.Empty;
            Identifier = 0;
            NamespaceUri = null;
            Version = string.Empty;
            PublicationDate = DateTime.MinValue;
            LastModifiedDate = DateTime.MinValue;
        }

        /// <summary>Gets or sets the nodeset XML.</summary>
        /// <value>The nodeset XML.</value>
        [JsonProperty("nodesetXml")]
        public string NodesetXml { get; set; }

        /// <summary>Gets or sets the identifier.</summary>
        /// <value>The identifier.</value>
        [JsonProperty("identifier")]
        public uint Identifier { get; set; }

        /// <summary>Gets or sets the namespace URI.</summary>
        /// <value>The namespace URI.</value>
        [JsonProperty("namespaceUri")]
        public Uri NamespaceUri { get; set; }

        /// <summary>Gets or sets the version.</summary>
        /// <value>The version.</value>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>Gets or sets the publication date.</summary>
        /// <value>The publication date.</value>
        [JsonProperty("publicationDate")]
        public DateTime PublicationDate { get; set; }

        /// <summary>Gets or sets the last modified date.</summary>
        /// <value>The last modified date.</value>
        [JsonProperty("lastModifiedDate")]
        public DateTime LastModifiedDate { get; set; }
    }
}
