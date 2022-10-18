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
    using System.Collections.Generic;
    using Newtonsoft.Json;
    /// <summary>GraphQL Result for nodeset queries</summary>
    public class UANodesetResult
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <value>The identifier.</value>
        [JsonProperty(PropertyName = "nodesetId")]
        public uint Id { get; set; }

        /// <summary>Gets or sets the title.</summary>
        /// <value>The title.</value>
        [JsonProperty(PropertyName = "nodesetTitle")]
        public string Title { get; set; }

        /// <summary>Gets or sets the contributor.</summary>
        /// <value>The contributor.</value>
        [JsonProperty(PropertyName = "orgName")]
        public string Contributor { get; set; }

        /// <summary>Gets or sets the license.</summary>
        /// <value>The license.</value>
        [JsonProperty(PropertyName = "license")]
        public string License { get; set; }

        /// <summary>Gets or sets the version.</summary>
        /// <value>The version.</value>
        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }

        /// <summary>Gets or sets the publication date.</summary>
        /// <value>The creation time.</value>
        [JsonProperty(PropertyName = "publicationDate")]
        public System.DateTime? PublicationDate { get; set; }

        /// <summary>
        /// Gets or sets the namespace Uri.
        /// </summary>
        [JsonProperty(PropertyName = "nodesetNamespaceUri")]
        public string NameSpaceUri { get; set; }

        /// <summary>
        /// Validation status of the nodeset
        /// </summary>
        public string ValidationStatus { get; set; }
        /// <summary>
        /// Indicates the nodesets that this nodesets depends on
        /// </summary>
        public List<RequiredModelInfo> RequiredNodesets { get; set; }

        /// <summary>
        /// Copyright
        /// </summary>
        public string CopyrightText { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Category
        /// </summary>
        public Category Category { get; set; }

        /// <summary>
        /// Link to additional documentation, specifications, GitHub, etc.
        /// For example, If the address space is based on a standard or official UA Information Model, this links to the standard or the OPC specification URL.
        /// </summary>
        public Uri DocumentationUrl { get; set; }

        /// <summary>
        /// Link to an icon that can be used to represent this node set
        /// </summary>
        public Uri IconUrl { get; set; }

        /// <summary>
        /// Link to the license text
        /// </summary>
        public Uri LicenseUrl { get; set; }

        /// <summary>
        /// Keywords
        /// </summary>
        public string[] Keywords { get; set; }

        /// <summary>
        /// Link to purchasing information
        /// </summary>
        public Uri PurchasingInformationUrl { get; set; }

        /// <summary>
        /// Link to Release Notes
        /// </summary>
        public Uri ReleaseNotesUrl { get; set; }

        /// <summary>
        /// Link to test specification
        /// </summary>
        public Uri TestSpecificationUrl { get; set; }

        /// <summary>
        /// Supported ISO language codes
        /// </summary>
        public string[] SupportedLocales { get; set; }

        /// <summary>
        /// Number of downloads
        /// </summary>
        public uint NumberOfDownloads { get; set; }

        /// <summary>
        /// Additional properties
        /// </summary>
        public UAProperty[] AdditionalProperties { get; set; }
    }
}
