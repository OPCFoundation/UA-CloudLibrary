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

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Opc.Ua.Cloud.Library.Models
{
    /// <summary>
    /// Lic info
    /// </summary>
    public enum License
    {
        /// <summary>
        /// MIT License
        /// </summary>
        MIT,

        /// <summary>
        /// Apache License 2.0
        /// </summary>
        ApacheLicense20,

        /// <summary>
        /// Custom License
        /// </summary>
        Custom
    }

    /// <summary>
    /// Namespace metadata for a UA Information Model
    /// </summary>
    public class UANameSpaceMetadata
    {
        /// <summary>Gets or sets the title.</summary>
        /// <value>The title.</value>
        [JsonRequired]
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>Gets or sets the contributor.</summary>
        /// <value>The contributor.</value>
        [JsonRequired]
        [JsonProperty("contributor")]
        public Organisation Contributor { get; set; }

        /// <summary>Gets or sets the license.</summary>
        /// <value>The license.</value>
        [JsonProperty("license")]
        public string License { get; set; }

        /// <summary>Gets or sets the copyright text.</summary>
        /// <value>The copyright text.</value>
		[JsonRequired]
        [JsonProperty("copyrightText")]
        public string CopyrightText { get; set; }

        /// <summary>Gets or sets the description.</summary>
        /// <value>The description.</value>
		[JsonRequired]
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>Gets or sets the category.</summary>
        /// <value>The category.</value>
		[JsonRequired]
        [JsonProperty("category")]
        public Category Category { get; set; }

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

        /// <summary>
        /// Custom properties that are not part of the standard metadata
        /// </summary>
        public UAProperty[] AdditionalProperties { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public UANameSpaceMetadata()
        {
            Title = string.Empty;
            License = null;
            CopyrightText = string.Empty;
            Contributor = new Organisation();
            Description = string.Empty;
            Category = new Category();
            DocumentationUrl = null;
            IconUrl = null;
            LicenseUrl = null;
            Keywords = Array.Empty<string>();
            PurchasingInformationUrl = null;
            ReleaseNotesUrl = null;
            TestSpecificationUrl = null;
            SupportedLocales = Array.Empty<string>();
            AdditionalProperties = null;
        }

        /// <summary>
        /// stringify
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Title} {Contributor} {Category}";
        }
    }

    /// <summary>
    /// The full namespace
    /// </summary>
    public class UANameSpace : UANameSpaceMetadata
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public UANameSpace()
        {
            CreationTime = null;
            NumberOfDownloads = 0;
            Nodeset = new Nodeset();
        }

        /// <summary>
        /// The nodeset for this namespace
        /// </summary>
        [JsonRequired]
        public Nodeset Nodeset { get; set; }

        /// <summary>
        /// The time the nodeset was uploaded to the cloud library
        /// </summary>
        [JsonProperty("creationTime")]
        public DateTime? CreationTime { get; set; }

        /// <summary>
        /// Number of downloads of the nodeset
        /// </summary>
        public uint NumberOfDownloads { get; set; }
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
        [JsonRequired]
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

        /// <summary>
        /// Equals method to compare two Organisation objects based on their Name property.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                Organisation org = (Organisation)obj;
                return Name.Equals(org.Name, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <remarks>The hash code is generated based on the <see cref="Name"/> property using ordinal
        /// string comparison.</remarks>
        /// <returns>An integer hash code representing the current object.</returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode(StringComparison.Ordinal);
        }
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
		[JsonRequired]
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

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <remarks>Two objects are considered equal if they are of the same type and their <c>Name</c>
        /// properties are equal using ordinal comparison.</remarks>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><see langword="true"/> if the specified object is equal to the current object; otherwise, <see
        /// langword="false"/>.</returns>
        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                Category org = (Category)obj;
                return Name.Equals(org.Name, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <returns>An integer hash code representing the current object, calculated using the <see
        /// cref="StringComparison.Ordinal"/> comparison for the <see cref="Name"/> property.</returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode(StringComparison.Ordinal);
        }
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
            ValidationStatus = null;
            RequiredModels = new List<RequiredModelInfo>();
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

        /// <summary>Gets or sets the validation status.</summary>
        /// <value>The validation status.</value>
        [JsonProperty("validationStatus")]
        public string ValidationStatus { get; set; }

        /// <summary>
        /// Nodesets that this nodeset depends on
        /// </summary>
        [JsonProperty("requiredModels")]
        public List<RequiredModelInfo> RequiredModels { get; set; }
    }

    /// <summary>
    /// Contains information about dependencies of a nodeset
    /// </summary>
    public class RequiredModelInfo
    {
        /// <summary>
        /// The namespace URI of the dependency
        /// </summary>
        public string NamespaceUri { get; set; }

        /// <summary>
        /// The minimum required publication date of the dependency
        /// </summary>
        public DateTime? PublicationDate { get; set; }

        /// <summary>
        /// The informational version of the dependency
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// The best match currently available in the cloud library. null if no match (no nodeset for this namespace uri or only node sets with older publication dates).
        /// </summary>
        public Nodeset AvailableModel { get; set; }
    }
}
